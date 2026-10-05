namespace MonitorCpu
{
// Dato simple de batería. Sin lógica, solo guarda el estado.
public sealed class InfoBateria
{
    public readonly int Porcentaje;
    public readonly bool EstaCargando;
    public readonly bool TieneBateria;

    public InfoBateria(int porcentaje, bool estaCargando, bool tieneBateria)
    {
        Porcentaje = porcentaje;
        EstaCargando = estaCargando;
        TieneBateria = tieneBateria;
    }
}
}
