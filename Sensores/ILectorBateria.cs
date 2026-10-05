namespace MonitorCpu
{
// Contrato para leer batería. Para añadir otro sensor futuro
// se crea una interfaz igual de pequeña (ej: ILectorTemperatura).
public interface ILectorBateria
{
    InfoBateria Leer();
}
}
