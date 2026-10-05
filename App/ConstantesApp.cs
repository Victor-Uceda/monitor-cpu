using System.Drawing;

namespace MonitorCpu
{

// Todos los números y textos ajustables viven aquí.
// Así no hay "magic numbers" regados por el código.
public static class ConstantesApp
{
    public const string NombreApp = "MonitorCpu";

    // Mutex para instancia única: si ya está abierto, el segundo .exe no abre ventana.
    // Base sin prefijo: Program prueba Global\ y luego Local\.
    public const string NombreMutex = "MonitorCpu_UnicaInstancia";

    // Muestreo: 1 vez por segundo (suficiente y casi 0% CPU).
    public const int IntervaloMuestreoMs = 1000;

    // Si la ventana está oculta (doble clic en bandeja), se mide cada 5s:
    // nadie mira el widget, solo se mantiene el tooltip al día.
    public const int IntervaloOcultoMs = 5000;

    // La batería cambia lento: se lee 1 de cada 30 ticks (~30s) y se cachea.
    public const int CadaCuantosTicksBateria = 30;

    // Tooltip de bandeja: NotifyIcon corta a 63 caracteres, se refresca
    // 1 de cada 3 ticks y solo si cambió (cada asignación llama al shell).
    public const int CadaCuantosTicksTooltip = 3;
    public const int LongitudMaximaTooltip = 63;

    // La gráfica guarda 60 puntos = últimos 60 segundos.
    public const int CapacidadHistorial = 60;

    // Tamaño del widget para el segundo monitor.
    public const int AnchoVentana = 320;
    public const int AltoVentana = 260;
    public const int Margen = 12;
    public const int AltoBotonCerrar = 26;
    public const int AnchoBotonVentana = 30;
    public const int MargenBotonVentana = 4;

    // Espaciado vertical entre bloques (botones -> etiquetas, batería -> gráfica).
    public const int EspaciadoBloques = 4;
    public const int AltoEtiquetaCpu = 50;
    public const int AltoEtiquetaUso = 22;
    public const int AltoEtiquetaRam = 22;
    public const int AltoEtiquetaBateria = 24;
    public const int AltoGrafica = 80;

    public const int TamanoFuenteCpu = 26;
    public const int TamanoFuenteUso = 12;
    public const int TamanoFuenteBateria = 12;

    // A partir de aquí la batería se muestra en rojo.
    public const int UmbralBateriaBaja = 20;

    // Rango de % válido para sensores (se recorta a este intervalo).
    public const int PorcentajeMinimo = 0;
    public const int PorcentajeMaximo = 100;

    // 1 GB en bytes (para pasar la RAM de GlobalMemoryStatusEx a GB).
    public const double BytesPorGB = 1024.0 * 1024.0 * 1024.0;

    // Gráfica: grosor de línea, rango mínimo (si no, dibuja plano),
    // margen sobre el rango real y relleno vertical del dibujo.
    public const float GrosorLineaGrafica = 2;
    public const double RangoMinimoGrafica = 0.05;
    public const double MargenGraficaFactor = 0.2;
    public const int RellenoVerticalGrafica = 4;
    public const int BordeInferiorGrafica = 2;

    // A partir de aquí CPU/RAM se muestran en rojo (carga alta).
    public const int UmbralCpuAlto = 80;
    public const int UmbralRamAlta = 90;

    public const string TextoCargando = "Cargando";
    public const string TextoBateria = "Bateria";
    public const string TextoSinBateria = "Sin bateria";

    // Contador de Windows que usa el Admin. de tareas para los GHz.
    public const string CategoriaContador = "Processor Information";
    public const string NombreContador = "% Processor Performance";
    public const string InstanciaContador = "_Total";

    // Si no se puede leer la frecuencia base, se usa esta.
    public const int FrecuenciaBasePorDefectoMhz = 2600;

    // Consulta WMI para la frecuencia base (MaxClockSpeed viene en MHz).
    public const string ConsultaWmiFrecuenciaBase = "SELECT MaxClockSpeed FROM Win32_Processor";

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
