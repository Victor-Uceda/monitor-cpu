using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MonitorCpu
{

// Lee % de uso de CPU como el Administrador de tareas moderno (Win10/11).
// Ruta rápida: PerformanceCounter (barato tras el init, 1 llamada por segundo).
// Fallback: GetSystemTimes (1 llamada al kernel, sin WMI pesado cada segundo).
public sealed class LectorUsoCpu : ILectorUsoCpu
{
    private readonly PerformanceCounter contador;
    private bool terminado;

    // Estado para el fallback GetSystemTimes (deltas entre ticks).
    private long prevOcioso;
    private long prevKernel;
    private long prevUsuario;
    private bool tienePrevio;

    public LectorUsoCpu()
    {
        contador = CrearContador();

        // Este contador necesita 2 muestras; la primera se descarta aquí.
        if (contador != null)
        {
            try
            {
                contador.NextValue();
            }
            catch
            {
                // Si la primera lectura falla, el tick usará el fallback.
            }
        }
    }

    private static PerformanceCounter CrearContador()
    {
        // Orden: primero el del Admin. moderno, luego el clásico.
        // Utility tiene en cuenta Turbo/SpeedStep; Time solo mide ocupación.
        string[][] nombresPosibles = new string[][]
        {
            new string[] { "Processor Information", "% Processor Utility" },
            new string[] { "Processor", "% Processor Time" },
            new string[] { "Procesador", "% de tiempo de procesador" }
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

    public double LeerPorcentaje()
    {
        // Ruta rápida: contador de Windows (igual que Admin. de tareas).
        // Solo 1 NextValue() por segundo: coste casi 0.
        if (contador != null)
        {
            try
            {
                double valor = contador.NextValue();
                if (valor < 0)
                {
                    return 0;
                }
                // Utility puede pasar de 100 con Turbo: el Admin. lo capa a 100.
                if (valor > 100)
                {
                    return 100;
                }
                return valor;
            }
            catch
            {
                // Contador roto/permisos: se cae al fallback barato.
            }
        }
        return LeerPorTiemposSistema();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME ocioso, out FILETIME kernel, out FILETIME usuario);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint Baja;
        public uint Alta;
    }

    private static long AEntero(FILETIME t)
    {
        return ((long)t.Alta << 32) | t.Baja;
    }

    private double LeerPorTiemposSistema()
    {
        try
        {
            FILETIME ocioso;
            FILETIME kernel;
            FILETIME usuario;
            if (!GetSystemTimes(out ocioso, out kernel, out usuario))
            {
                return 0;
            }

            long ahoraOcioso = AEntero(ocioso);
            long ahoraKernel = AEntero(kernel);
            long ahoraUsuario = AEntero(usuario);

            if (!tienePrevio)
            {
                // Primera muestra: no hay delta todavía.
                prevOcioso = ahoraOcioso;
                prevKernel = ahoraKernel;
                prevUsuario = ahoraUsuario;
                tienePrevio = true;
                return 0;
            }

            long difKernel = ahoraKernel - prevKernel;
            long difUsuario = ahoraUsuario - prevUsuario;
            long difOcioso = ahoraOcioso - prevOcioso;

            prevOcioso = ahoraOcioso;
            prevKernel = ahoraKernel;
            prevUsuario = ahoraUsuario;

            long total = difKernel + difUsuario;
            if (total <= 0)
            {
                return 0;
            }

            // kernel incluye tiempo ocioso: lo útil = total - ocioso.
            double uso = (double)(total - difOcioso) * 100.0 / (double)total;
            if (uso < 0)
            {
                return 0;
            }
            if (uso > 100)
            {
                return 100;
            }
            return uso;
        }
        catch
        {
            return 0;
        }
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
