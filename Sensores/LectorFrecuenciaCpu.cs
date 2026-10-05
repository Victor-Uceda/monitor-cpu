using System;
using System.Diagnostics;
using System.Management;

namespace MonitorCpu
{

// Lee los GHz como el Administrador de tareas:
// GHz = (% rendimiento del procesador / 100) * frecuencia base.
public sealed class LectorFrecuenciaCpu : ILectorFrecuenciaCpu
{
    private readonly PerformanceCounter contador;
    private readonly double ghzBase;
    private bool terminado;

    public LectorFrecuenciaCpu()
    {
        // La frecuencia base se lee una sola vez (no en cada tick).
        ghzBase = LeerFrecuenciaBaseMhz() / 1000.0;

        // Windows ES y EN usan nombres distintos: se prueban ambos.
        contador = CrearContador();

        // Este contador necesita 2 muestras; la primera se descarta aquí.
        if (contador != null)
        {
            contador.NextValue();
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
        // Sin contador (VM, permisos, idioma no probado): valor estable legible.
        if (contador == null)
        {
            return ghzBase;
        }
        double rendimiento = contador.NextValue();
        return (rendimiento / 100.0) * ghzBase;
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
        if (!terminado)
        {
            if (contador != null)
            {
                contador.Dispose();
            }
            terminado = true;
        }
    }
}
}
