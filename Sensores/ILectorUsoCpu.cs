namespace MonitorCpu
{
// Contrato para leer % de uso de CPU (0-100).
// Separado del lector de GHz: una cosa es velocidad, otra es carga.
public interface ILectorUsoCpu : System.IDisposable
{
    double LeerPorcentaje();
}
}
