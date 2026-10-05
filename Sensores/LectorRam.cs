using System;
using System.Runtime.InteropServices;

namespace MonitorCpu
{

// Lee RAM con GlobalMemoryStatusEx (1 llamada al kernel).
// Antes usaba WMI cada segundo: COM + GC + ~10-50ms. Ahora ~microsegundos y 0 basura.
// No depende del idioma (Memory/Memoria) ni de System.Management en el tick.
public sealed class LectorRam : ILectorRam
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct EstadoMemoria
    {
        public uint Longitud;
        public uint Carga;
        public ulong TotalFisica;
        public ulong LibreFisica;
        public ulong TotalPaginada;
        public ulong LibrePaginada;
        public ulong TotalVirtual;
        public ulong LibreVirtual;
        public ulong LibreVirtualExtendida;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref EstadoMemoria estado);

    public InfoRam Leer()
    {
        try
        {
            EstadoMemoria estado = new EstadoMemoria();
            estado.Longitud = (uint)Marshal.SizeOf(typeof(EstadoMemoria));
            if (GlobalMemoryStatusEx(ref estado) && estado.TotalFisica > 0)
            {
                ulong usada = estado.TotalFisica - estado.LibreFisica;
                double usadaGB = usada / ConstantesApp.BytesPorGB;
                double totalGB = estado.TotalFisica / ConstantesApp.BytesPorGB;
                // Carga ya es el % (0-100) calculado por Windows: se usa directo.
                return new InfoRam(estado.Carga, usadaGB, totalGB);
            }
        }
        catch
        {
            // API del kernel casi nunca falla. A propósito sin log.
        }
        return InfoRam.SinDatos();
    }
}
}
