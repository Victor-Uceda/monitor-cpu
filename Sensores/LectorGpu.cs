using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MonitorCpu
{

// Lee % total de GPU sumando "GPU Engine(*)\Utilization Percentage".
// Como el Admin. de tareas: la suma de todos los motores es el total.
//
// Arranque rapido (opcion A): el constructor vuelve en ~0ms y la
// enumeracion pesada (GetInstanceNames 1-3s) se hace en un hilo de fondo.
// LeerPorcentaje() nunca bloquea: devuelve -1 ("GPU: --") hasta que
// termina el primer refresco, y luego el ultimo valor conocido.
public sealed class LectorGpu : ILectorGpu
{
    private List<PerformanceCounter> contadores = new List<PerformanceCounter>();
    private readonly object candado = new object();
    private int ticks;
    private bool terminado;
    private bool refrescando;
    private double ultimoTotal;
    private bool tieneUltimo;

    public LectorGpu()
    {
        // Enumeracion pesada en fondo para pintar la ventana al instante.
        PedirRefresco();
    }

    private void PedirRefresco()
    {
        lock (candado)
        {
            if (terminado || refrescando)
            {
                return;
            }
            refrescando = true;
        }
        ThreadPool.QueueUserWorkItem(RefrescarEnFondo);
    }

    private void RefrescarEnFondo(object estado)
    {
        List<PerformanceCounter> nuevos = new List<PerformanceCounter>();
        try
        {
            PerformanceCounterCategory categoria = new PerformanceCounterCategory("GPU Engine");
            string[] instancias = categoria.GetInstanceNames();
            foreach (string nombre in instancias)
            {
                try
                {
                    PerformanceCounter c = new PerformanceCounter("GPU Engine", "Utilization Percentage", nombre, true);
                    c.NextValue();
                    nuevos.Add(c);
                }
                catch
                {
                    // Instancia fugaz (proceso cerrado): se salta.
                }
            }
        }
        catch
        {
            // Sin GPU o sin contador: se queda vacío y devuelve -1.
            foreach (PerformanceCounter c in nuevos)
            {
                try
                {
                    c.Dispose();
                }
                catch
                {
                }
            }
            nuevos.Clear();
        }

        lock (candado)
        {
            if (terminado)
            {
                foreach (PerformanceCounter c in nuevos)
                {
                    try
                    {
                        c.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                foreach (PerformanceCounter c in contadores)
                {
                    try
                    {
                        c.Dispose();
                    }
                    catch
                    {
                    }
                }
                contadores = nuevos;
            }
            refrescando = false;
        }
    }

    public double LeerPorcentaje()
    {
        if (terminado)
        {
            return -1;
        }
        try
        {
            List<PerformanceCounter> copia;
            double ultimo;
            bool tiene;
            lock (candado)
            {
                copia = new List<PerformanceCounter>(contadores);
                ultimo = ultimoTotal;
                tiene = tieneUltimo;
            }

            // Sin contadores aun: primer refresco en curso -> "GPU: --".
            if (copia.Count == 0)
            {
                PedirRefresco();
                return -1;
            }

            // Refresco barato: 1 de cada 5 ticks (~5s) para ver procesos nuevos.
            // No bloquea: se hace en fondo y este tick usa la lista vieja.
            ticks++;
            if ((ticks % 5) == 0)
            {
                PedirRefresco();
            }

            double total = 0;
            foreach (PerformanceCounter c in copia)
            {
                try
                {
                    total += c.NextValue();
                }
                catch
                {
                    // Contador muerto: se refresca en el próximo ciclo.
                }
            }
            if (total < 0)
            {
                total = 0;
            }
            if (total > 100)
            {
                total = 100;
            }
            lock (candado)
            {
                ultimoTotal = total;
                tieneUltimo = true;
            }
            return total;
        }
        catch
        {
            return -1;
        }
    }

    public void Dispose()
    {
        List<PerformanceCounter> viejos = null;
        lock (candado)
        {
            if (!terminado)
            {
                terminado = true;
                viejos = contadores;
                contadores = new List<PerformanceCounter>();
            }
        }
        if (viejos != null)
        {
            foreach (PerformanceCounter c in viejos)
            {
                try
                {
                    c.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
}
