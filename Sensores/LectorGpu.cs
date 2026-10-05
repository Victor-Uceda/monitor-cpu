using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MonitorCpu
{

// Lee % total de GPU sumando "GPU Engine(*)\Utilization Percentage".
// Como el Admin. de tareas: la suma de todos los motores es el total.
//
// OJO rendimiento: en un PC normal hay 100-250 instancias (una por
// proceso y motor: 3D, Copy, VideoDecode...). Medirlas todas con
// NextValue() cuesta ~200-300ms de CPU por barrido. Hacerlo cada
// segundo = ~10% de CPU del proceso solo para la GPU. Por eso:
//  - Se mide como maximo 1 vez cada 3s y el resto se devuelve cache.
//  - La lista de instancias (GetInstanceNames ~500ms) se refresca
//    cada 60s en fondo, no cada 5s.
//  - La ruta caliente no aloca ni bloquea: lee un array snapshot.
public sealed class LectorGpu : ILectorGpu
{
    // No medir mas de 1 vez cada 3s: 300ms/3s ~= 2-3% de un core en el
    // peor caso, frente al ~30% de un core midiendo cada segundo.
    private const int IntervaloMinMedicionMs = 3000;

    // Las instancias pid_* cambian al abrir/cerrar programas, pero con
    // refrescar cada 60s basta. Cada refresco cuesta ~0.5-1s de CPU.
    private const int IntervaloRefrescoInstanciasSeg = 60;

    private readonly object candado = new object();
    private volatile PerformanceCounter[] foto = new PerformanceCounter[0];
    private bool terminado;
    private bool refrescando;

    private double ultimoTotal;
    private bool tieneUltimo;
    private long ultimoMedicionTicks;
    private long ultimoRefrescoTicks;

    public LectorGpu()
    {
        // Enumeracion pesada en fondo para pintar la ventana al instante.
        PedirRefresco(true);
    }

    private void PedirRefresco(bool forzar)
    {
        long ahora = DateTime.UtcNow.Ticks;
        lock (candado)
        {
            if (terminado || refrescando)
            {
                return;
            }
            if (!forzar && tieneUltimo && (ahora - ultimoRefrescoTicks) < TimeSpan.TicksPerSecond * IntervaloRefrescoInstanciasSeg)
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
                PerformanceCounter[] viejos = foto;
                foto = nuevos.ToArray();
                ultimoRefrescoTicks = DateTime.UtcNow.Ticks;
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
            // Ruta caliente: sin lock, sin alloc. Solo se mide si el
            // cache tiene mas de 3s; el resto de ticks es gratis.
            PerformanceCounter[] copia = foto;
            long ahora = DateTime.UtcNow.Ticks;

            if (tieneUltimo && (ahora - ultimoMedicionTicks) < IntervaloMinMedicionMs * TimeSpan.TicksPerMillisecond)
            {
                return ultimoTotal;
            }

            if (copia.Length == 0)
            {
                PedirRefresco(false);
                return tieneUltimo ? ultimoTotal : -1;
            }

            // Toca renovar instancias cada 60s: en fondo, sin bloquear.
            PedirRefresco(false);

            double total = 0;
            foreach (PerformanceCounter c in copia)
            {
                try
                {
                    total += c.NextValue();
                }
                catch
                {
                    // Contador muerto: se renueva en el proximo refresco.
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
            ultimoTotal = total;
            tieneUltimo = true;
            ultimoMedicionTicks = ahora;
            return total;
        }
        catch
        {
            return tieneUltimo ? ultimoTotal : -1;
        }
    }

    public void Dispose()
    {
        PerformanceCounter[] viejos = null;
        lock (candado)
        {
            if (!terminado)
            {
                terminado = true;
                viejos = foto;
                foto = new PerformanceCounter[0];
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
