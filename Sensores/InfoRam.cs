namespace MonitorCpu
{
// Dato inmutable de RAM: % usada + GB usada/total.
// TieneDatos=false cuando WMI falló (no se muestra basura).
public struct InfoRam
{
    public readonly double Porcentaje;
    public readonly double UsadaGB;
    public readonly double TotalGB;
    public readonly bool TieneDatos;

    public InfoRam(double porcentaje, double usadaGB, double totalGB)
    {
        Porcentaje = porcentaje;
        UsadaGB = usadaGB;
        TotalGB = totalGB;
        TieneDatos = true;
    }

    private InfoRam(bool tieneDatos)
    {
        Porcentaje = 0;
        UsadaGB = 0;
        TotalGB = 0;
        TieneDatos = tieneDatos;
    }

    public static InfoRam SinDatos()
    {
        return new InfoRam(false);
    }
}
}
