using System;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace MonitorCpu
{

// Lee los GHz como el Administrador de tareas:
// GHz = (% rendimiento del procesador / 100) * frecuencia base.
//
// Arranque rapido (opcion A): el constructor vuelve en ~0ms con el valor
// por defecto y la WMI + el contador se inicializan en un hilo de fondo.
// Mientras tanto LeerGHz() devuelve la base (estable y legible).
public sealed class LectorFrecuenciaCpu : ILectorFrecuenciaCpu
{
    private PerformanceCounter contador;
    private double ghzBase;
    private readonly object candado = new object();
    private bool terminado;

    public LectorFrecuenciaCpu()
    {
        // Valor inmediato para pintar la ventana sin esperar a WMI.
        ghzBase = ConstantesApp.FrecuenciaBasePorDefectoMhz / 1000.0;

        // Trabajo pesado (WMI 0.5-1.5s + PerformanceCounter) en fondo.
        ThreadPool.QueueUserWorkItem(InicializarEnFondo);
    }

    private void InicializarEnFondo(object estado)
    {
        if (terminado)
        {
            return;
        }

        // La frecuencia base se lee una sola vez (no en cada tick).
        int mhz = LeerFrecuenciaBaseMhz();

        // Windows ES y EN usan nombres distintos: se prueban ambos.
        PerformanceCounter nuevo = CrearContador();

        // Este contador necesita 2 muestras; la primera se descarta aquí.
        if (nuevo != null)
        {
            try
            {
                nuevo.NextValue();
            }
            catch
            {
                // Contador roto: se descarta y se usa la base.
                try
                {
                    nuevo.Dispose();
                }
                catch
                {
                }
                nuevo = null;
            }
        }

        lock (candado)
        {
            if (terminado)
            {
                if (nuevo != null)
                {
                    try
                    {
                        nuevo.Dispose();
                    }
                    catch
                    {
                    }
                }
                return;
            }
            ghzBase = mhz / 1000.0;
            contador = nuevo;
        }
    }

    // Prueba catálogo ES primero, luego EN. Si ninguno existe, devuelve null.
    private static PerformanceCounter CrearContador()
    {
        string[][] nombresPosibles = new string[][]
        {
            // "Información" sin ó literal: evita problemas de encoding del compilador.
            new string[] { "Informaci\u00F3n del procesador", "% de rendimiento del procesador" },
            new string[] { "Processor Information", "% Processor Performance" }
        };

        foreach (string[] nombres in nombresPosibles)
        {
            try
            {
                return new PerformanceCounter(nombres[0], nombres[1], "_Total", true);
            }
            catch
            {
                // No existe en este idioma: se prueba el siguiente.
            }
        }
        return null;
    }

    public double LeerGHz()
    {
        PerformanceCounter actual;
        double baseActual;
        lock (candado)
        {
            actual = contador;
            baseActual = ghzBase;
        }
        // Sin contador todavia (init en fondo) o nunca (VM, permisos):
        // valor estable legible.
        if (actual == null)
        {
            return baseActual;
        }
        try
        {
            double rendimiento = actual.NextValue();
            return (rendimiento / 100.0) * baseActual;
        }
        catch
        {
            // Contador muerto a mitad de sesion: se devuelve la base.
            return baseActual;
        }
    }

    private static int LeerFrecuenciaBaseMhz()
    {
        try
        {
            // MaxClockSpeed viene en MHz y casi nunca cambia.
            using (ManagementObjectSearcher busqueda = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor"))
            {
                foreach (ManagementObject cpu in busqueda.Get())
                {
                    object valor = cpu["MaxClockSpeed"];
                    if (valor != null)
                    {
                        return Convert.ToInt32(valor);
                    }
                }
            }
        }
        catch
        {
            // Si WMI falla, se usa el valor por defecto. A propósito sin log.
        }
        return ConstantesApp.FrecuenciaBasePorDefectoMhz;
    }

    public void Dispose()
    {
        PerformanceCounter actual = null;
        lock (candado)
        {
            if (!terminado)
            {
                actual = contador;
                contador = null;
                terminado = true;
            }
        }
        if (actual != null)
        {
            try
            {
                actual.Dispose();
            }
            catch
            {
            }
        }
    }
}
}
