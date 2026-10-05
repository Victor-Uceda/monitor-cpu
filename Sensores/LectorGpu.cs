using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonitorCpu
{

// Lee % total de GPU sumando "GPU Engine(*)\Utilization Percentage".
// Como el Admin. de tareas: la suma de todos los motores es el total.
// Barato: contadores creados una vez, solo se refrescan cada ~5s
// (los pid_* cambian cuando se abren/cierran programas).
public sealed class LectorGpu : ILectorGpu
{
    private List<PerformanceCounter> contadores = new List<PerformanceCounter>();
    private int ticks;
    private bool terminado;

    public LectorGpu()
    {
        Refrescar();
    }

    private void Refrescar()
    {
        Limpiar();
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
                    contadores.Add(c);
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
        }
    }

    private void Limpiar()
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
        contadores.Clear();
    }

    public double LeerPorcentaje()
    {
        if (terminado)
        {
            return -1;
        }
        try
        {
            // Refresco barato: 1 de cada 5 ticks (~5s) para ver procesos nuevos.
            ticks++;
            if (contadores.Count == 0 || (ticks % 5 == 0))
            {
                Refrescar();
            }
            if (contadores.Count == 0)
            {
                return -1;
            }
            double total = 0;
            foreach (PerformanceCounter c in contadores)
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
                return 0;
            }
            if (total > 100)
            {
                return 100;
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
        if (!terminado)
        {
            Limpiar();
            terminado = true;
        }
    }
}
}
