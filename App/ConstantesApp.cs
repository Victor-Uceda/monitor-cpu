using System.Drawing;

namespace MonitorCpu
{

// Todos los números y textos ajustables viven aquí.
// Así no hay "magic numbers" regados por el código.
public static class ConstantesApp
{
    public const string NombreApp = "MonitorCpu";

    // Mutex para instancia única: si ya está abierto, el segundo .exe no abre ventana.
    public const string NombreMutex = @"Local\MonitorCpu_UnicaInstancia";

    // Muestreo: 1 vez por segundo (suficiente y casi 0% CPU).
    public const int IntervaloMuestreoMs = 1000;

    // Si la ventana está oculta (doble clic en bandeja), se mide cada 5s:
    // nadie mira el widget, solo se mantiene el tooltip al día.
    public const int IntervaloOcultoMs = 5000;

    // La batería cambia lento: se lee 1 de cada 30 ticks (~30s) y se cachea.
    public const int CadaCuantosTicksBateria = 30;

    // La gráfica guarda 60 puntos = últimos 60 segundos.
    public const int CapacidadHistorial = 60;

    // Tamaño del widget para el segundo monitor.
    public const int AnchoVentana = 320;
    public const int AltoVentana = 282;
    public const int Margen = 12;
    public const int AltoBotonCerrar = 26;
    public const int AltoEtiquetaCpu = 50;
    public const int AltoEtiquetaUso = 22;
    public const int AltoEtiquetaGpu = 22;
    public const int AltoEtiquetaRam = 22;
    public const int AltoEtiquetaBateria = 24;
    public const int AltoGrafica = 80;

    public const int TamanoFuenteCpu = 26;
    public const int TamanoFuenteUso = 12;
    public const int TamanoFuenteBateria = 12;
    public const int TamanoFuentePequena = 9;

    // A partir de aquí la batería se muestra en rojo.
    public const int UmbralBateriaBaja = 20;

    // A partir de aquí CPU/RAM/GPU se muestran en rojo (carga alta).
    public const int UmbralCpuAlto = 80;
    public const int UmbralRamAlta = 90;
    public const int UmbralGpuAlto = 80;

    public const string TextoCargando = "Cargando";
    public const string TextoBateria = "Bateria";
    public const string TextoSinBateria = "Sin bateria";

    // Contador de Windows que usa el Admin. de tareas para los GHz.
    public const string CategoriaContador = "Processor Information";
    public const string NombreContador = "% Processor Performance";
    public const string InstanciaContador = "_Total";

    // Si no se puede leer la frecuencia base, se usa esta.
    public const int FrecuenciaBasePorDefectoMhz = 2600;

    // Arranque automático con Windows (solo HKCU, no pide admin).
    public const string ClaveArranqueRegistro = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public const double OpacidadVentana = 0.92;

    // Colores del widget (se definen una vez).
    public static readonly Color ColorFondo = Color.FromArgb(30, 30, 30);
    public static readonly Color ColorTexto = Color.White;
    public static readonly Color ColorTextoSuave = Color.LightGray;
    public static readonly Color ColorVerde = Color.LimeGreen;
    public static readonly Color ColorRojo = Color.Tomato;
    public static readonly Color ColorLineaGrafica = Color.DodgerBlue;
}
}
