namespace MonitorCpu
{
// Contrato para leer % total de GPU (0-100).
// Devuelve -1 si no hay GPU o no se puede medir.
public interface ILectorGpu : System.IDisposable
{
    double LeerPorcentaje();
}
}
