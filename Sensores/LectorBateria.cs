using System.Windows.Forms;

namespace MonitorCpu
{

// Lee batería con la API de Windows (GetSystemPowerStatus).
// No usa WMI ni hace polling pesado: es una llamada casi gratis.
public sealed class LectorBateria : ILectorBateria
{
    public InfoBateria Leer()
    {
        PowerStatus estado = SystemInformation.PowerStatus;

        // PowerStatus da 0.0 a 1.0 (desconocido = -1). Se pasa a 0-100.
        int porcentaje = (int)(estado.BatteryLifePercent * 100.0f);

        // NoSystemBattery = PC de escritorio.
        bool tieneBateria = estado.BatteryLifePercent >= 0
            && porcentaje <= 100
            && estado.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery;

        if (!tieneBateria)
        {
            return new InfoBateria(0, false, false);
        }

        // Enchufado = cargando (o lleno). Simple y suficiente.
        bool estaCargando = estado.PowerLineStatus == PowerLineStatus.Online;
        return new InfoBateria(porcentaje, estaCargando, true);
    }
}
}
