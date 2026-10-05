using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorCpu
{

// Ventana widget: solo muestra datos, no mide nada.
// Recibe los lectores por constructor (fácil de probar y cambiar).
public sealed class VentanaPrincipal : Form
{
    private readonly ILectorFrecuenciaCpu lectorCpu;
    private readonly ILectorBateria lectorBateria;
    private readonly ILectorUsoCpu lectorUsoCpu;
    private readonly ILectorRam lectorRam;
    private readonly GestorArranque gestorArranque;

    private readonly Timer temporizador = new Timer();
    private readonly Label etiquetaCpu = new Label();
    private readonly Label etiquetaUsoCpu = new Label();
    private readonly Label etiquetaRam = new Label();
    private readonly Label etiquetaBateria = new Label();
    private readonly GraficaHistorial grafica = new GraficaHistorial();
    private readonly Button botonCerrar = new Button();
    private readonly Button botonMinimizar = new Button();
    private readonly NotifyIcon iconoBandeja = new NotifyIcon();
    private readonly ContextMenuStrip menuBandeja = new ContextMenuStrip();

    // Fuentes propias: se liberan en Dispose (handles GDI).
    private readonly Font fuenteCpu = new Font(FontFamily.GenericSansSerif, ConstantesApp.TamanoFuenteCpu, FontStyle.Bold);
    private readonly Font fuenteUso = new Font(FontFamily.GenericSansSerif, ConstantesApp.TamanoFuenteUso);
    private readonly Font fuenteBateria = new Font(FontFamily.GenericSansSerif, ConstantesApp.TamanoFuenteBateria);

    // Para arrastrar la ventana sin bordes.
    private bool arrastrando;
    private Point puntoArrastre;

    // Optimización: caché para no repetir trabajo caro ni redibujar igual.
    private int contadorTicks;
    private InfoBateria bateriaCacheada;
    private bool tieneBateriaCacheada;
    private string ultimoTextoBandeja = "";

    // Caché de lo ya mostrado: si el valor no cambió, ni se formatea
    // (string.Format cada segundo era basura en el caso estable común).
    private double ultimoGhz = double.NaN;
    private double ultimoUsoCpu = double.NaN;
    private bool tieneUltimaRam;
    private bool ultimaRamTiene;
    private double ultimaRamPct = double.NaN;
    private double ultimaRamUsada = double.NaN;
    private double ultimaRamTotal = double.NaN;
    private bool tieneUltimaBateria;
    private int ultimaBatPct;
    private bool ultimaBatCargando;
    private bool ultimaBatTiene;

    public VentanaPrincipal(ILectorFrecuenciaCpu lectorCpu, ILectorBateria lectorBateria, ILectorUsoCpu lectorUsoCpu, ILectorRam lectorRam, GestorArranque gestorArranque)
    {
        this.lectorCpu = lectorCpu;
        this.lectorBateria = lectorBateria;
        this.lectorUsoCpu = lectorUsoCpu;
        this.lectorRam = lectorRam;
        this.gestorArranque = gestorArranque;

        CrearVentana();
        CrearEtiquetas();
        CrearBandeja();
        CrearTemporizador();
    }

    private void CrearVentana()
    {
        // Widget sin bordes, siempre encima, semi-transparente.
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(ConstantesApp.AnchoVentana, ConstantesApp.AltoVentana);
        BackColor = ConstantesApp.ColorFondo;
        Opacity = ConstantesApp.OpacidadVentana;

        // Botón X para cerrar de verdad (no minimiza).
        botonCerrar.Text = "X";
        botonCerrar.ForeColor = ConstantesApp.ColorTexto;
        botonCerrar.BackColor = ConstantesApp.ColorFondo;
        botonCerrar.FlatStyle = FlatStyle.Flat;
        botonCerrar.Size = new Size(ConstantesApp.AnchoBotonVentana, ConstantesApp.AltoBotonCerrar);
        botonCerrar.Location = new Point(ConstantesApp.AnchoVentana - ConstantesApp.AnchoBotonVentana - ConstantesApp.MargenBotonVentana, ConstantesApp.MargenBotonVentana);
        botonCerrar.Click += AlCerrar;
        Controls.Add(botonCerrar);

        // Botón _ para minimizar a la bandeja (el widget se oculta).
        botonMinimizar.Text = "_";
        botonMinimizar.ForeColor = ConstantesApp.ColorTexto;
        botonMinimizar.BackColor = ConstantesApp.ColorFondo;
        botonMinimizar.FlatStyle = FlatStyle.Flat;
        botonMinimizar.Size = new Size(ConstantesApp.AnchoBotonVentana, ConstantesApp.AltoBotonCerrar);
        botonMinimizar.Location = new Point(ConstantesApp.AnchoVentana - ConstantesApp.AnchoBotonVentana - ConstantesApp.MargenBotonVentana - ConstantesApp.AnchoBotonVentana - ConstantesApp.MargenBotonVentana, ConstantesApp.MargenBotonVentana);
        botonMinimizar.Click += AlMinimizar;
        Controls.Add(botonMinimizar);

        // Arrastrar desde el fondo.
        MouseDown += AlEmpezarArrastre;
        MouseMove += AlArrastrar;
        MouseUp += AlTerminarArrastre;
    }

    private void CrearEtiquetas()
    {
        int y = ConstantesApp.AltoBotonCerrar + ConstantesApp.EspaciadoBloques;

        // Número grande de GHz.
        etiquetaCpu.Font = fuenteCpu;
        etiquetaCpu.ForeColor = ConstantesApp.ColorTexto;
        etiquetaCpu.BackColor = ConstantesApp.ColorFondo;
        etiquetaCpu.Location = new Point(ConstantesApp.Margen, y);
        etiquetaCpu.Size = new Size(ConstantesApp.AnchoVentana - ConstantesApp.Margen * 2, ConstantesApp.AltoEtiquetaCpu);
        etiquetaCpu.MouseDown += AlEmpezarArrastre;
        etiquetaCpu.MouseMove += AlArrastrar;
        etiquetaCpu.MouseUp += AlTerminarArrastre;
        Controls.Add(etiquetaCpu);
        y += ConstantesApp.AltoEtiquetaCpu;

        // % de uso de CPU.
        etiquetaUsoCpu.Font = fuenteUso;
        etiquetaUsoCpu.ForeColor = ConstantesApp.ColorTextoSuave;
        etiquetaUsoCpu.BackColor = ConstantesApp.ColorFondo;
        etiquetaUsoCpu.Location = new Point(ConstantesApp.Margen, y);
        etiquetaUsoCpu.Size = new Size(ConstantesApp.AnchoVentana - ConstantesApp.Margen * 2, ConstantesApp.AltoEtiquetaUso);
        etiquetaUsoCpu.MouseDown += AlEmpezarArrastre;
        etiquetaUsoCpu.MouseMove += AlArrastrar;
        etiquetaUsoCpu.MouseUp += AlTerminarArrastre;
        Controls.Add(etiquetaUsoCpu);
        y += ConstantesApp.AltoEtiquetaUso;

        // % + GB de RAM.
        etiquetaRam.Font = fuenteUso;
        etiquetaRam.ForeColor = ConstantesApp.ColorTextoSuave;
        etiquetaRam.BackColor = ConstantesApp.ColorFondo;
        etiquetaRam.Location = new Point(ConstantesApp.Margen, y);
        etiquetaRam.Size = new Size(ConstantesApp.AnchoVentana - ConstantesApp.Margen * 2, ConstantesApp.AltoEtiquetaRam);
        etiquetaRam.MouseDown += AlEmpezarArrastre;
        etiquetaRam.MouseMove += AlArrastrar;
        etiquetaRam.MouseUp += AlTerminarArrastre;
        Controls.Add(etiquetaRam);
        y += ConstantesApp.AltoEtiquetaRam;

        // Porcentaje + estado de batería.
        etiquetaBateria.Font = fuenteBateria;
        etiquetaBateria.ForeColor = ConstantesApp.ColorTextoSuave;
        etiquetaBateria.BackColor = ConstantesApp.ColorFondo;
        etiquetaBateria.Location = new Point(ConstantesApp.Margen, y);
        etiquetaBateria.Size = new Size(ConstantesApp.AnchoVentana - ConstantesApp.Margen * 2, ConstantesApp.AltoEtiquetaBateria);
        etiquetaBateria.MouseDown += AlEmpezarArrastre;
        etiquetaBateria.MouseMove += AlArrastrar;
        etiquetaBateria.MouseUp += AlTerminarArrastre;
        Controls.Add(etiquetaBateria);
        y += ConstantesApp.AltoEtiquetaBateria + ConstantesApp.EspaciadoBloques;

        // Gráfica de últimos 60 segundos.
        grafica.Location = new Point(ConstantesApp.Margen, y);
        grafica.Size = new Size(ConstantesApp.AnchoVentana - ConstantesApp.Margen * 2, ConstantesApp.AltoGrafica);
        Controls.Add(grafica);
    }

    private void CrearBandeja()
    {
        // Icono en bandeja: permite salir y activar autostart.
        iconoBandeja.Text = ConstantesApp.NombreApp;
        iconoBandeja.Icon = SystemIcons.Application;
        iconoBandeja.Visible = true;

        ToolStripMenuItem opcionArranque = new ToolStripMenuItem("Iniciar con Windows");
        opcionArranque.CheckOnClick = true;
        opcionArranque.Checked = gestorArranque.EstaActivado();
        opcionArranque.Click += AlCambiarArranque;

        ToolStripMenuItem opcionSalir = new ToolStripMenuItem("Salir");
        opcionSalir.Click += AlCerrar;

        ToolStripMenuItem opcionMostrar = new ToolStripMenuItem("Mostrar");
        opcionMostrar.Click += AlMostrar;

        menuBandeja.Items.Add(opcionMostrar);
        menuBandeja.Items.Add(opcionArranque);
        menuBandeja.Items.Add(opcionSalir);
        iconoBandeja.ContextMenuStrip = menuBandeja;
        iconoBandeja.DoubleClick += AlMostrarOcultar;
    }

    private void CrearTemporizador()
    {
        // Un solo timer: lee sensores y actualiza la vista.
        temporizador.Interval = ConstantesApp.IntervaloMuestreoMs;
        temporizador.Tick += AlTick;
        temporizador.Start();
    }

    private void AlTick(object sender, EventArgs e)
    {
        // Si la ventana está oculta nadie la mira: ritmo lento y sin tocar labels.
        // Ahorra CPU, GC y llamadas a la bandeja.
        bool oculta = !Visible;
        temporizador.Interval = oculta ? ConstantesApp.IntervaloOcultoMs : ConstantesApp.IntervaloMuestreoMs;

        // Si un sensor falla, la app no muere: muestra error y sigue viva.
        try
        {
            contadorTicks++;
            double ghz = lectorCpu.LeerGHz();
            double usoCpu = lectorUsoCpu.LeerPorcentaje();
            InfoRam ram = lectorRam.Leer();

            // La batería cambia lento: se cachea ~30s en vez de leer cada tick.
            if (!tieneBateriaCacheada || (contadorTicks % ConstantesApp.CadaCuantosTicksBateria == 0))
            {
                bateriaCacheada = lectorBateria.Leer();
                tieneBateriaCacheada = true;
            }
            InfoBateria bateria = bateriaCacheada;

            // La gráfica guarda historial aunque esté oculta (Enqueue barato,
            // sin Invalidate porque GraficaHistorial lo salta si no es visible).
            grafica.AgregarPunto(ghz);

            if (!oculta)
            {
                // Cada bloque solo formatea si el valor cambió desde el
                // último tick: en estado estable no hay string.Format ni
                // redibujado (PonTexto además lo protege).
                if (ghz != ultimoGhz)
                {
                    PonTexto(etiquetaCpu, string.Format("{0:F2} GHz", ghz));
                    ultimoGhz = ghz;
                }

                if (usoCpu != ultimoUsoCpu)
                {
                    PonTexto(etiquetaUsoCpu, string.Format("CPU: {0:F0}%", usoCpu));
                    etiquetaUsoCpu.ForeColor = usoCpu >= ConstantesApp.UmbralCpuAlto
                        ? ConstantesApp.ColorRojo
                        : ConstantesApp.ColorTextoSuave;
                    ultimoUsoCpu = usoCpu;
                }

                if (!tieneUltimaRam || ram.TieneDatos != ultimaRamTiene || ram.Porcentaje != ultimaRamPct || ram.UsadaGB != ultimaRamUsada || ram.TotalGB != ultimaRamTotal)
                {
                    if (!ram.TieneDatos)
                    {
                        PonTexto(etiquetaRam, "RAM: --");
                        etiquetaRam.ForeColor = ConstantesApp.ColorTextoSuave;
                    }
                    else
                    {
                        PonTexto(etiquetaRam, string.Format("RAM: {0:F0}% - {1:F1} / {2:F1} GB", ram.Porcentaje, ram.UsadaGB, ram.TotalGB));
                        etiquetaRam.ForeColor = ram.Porcentaje >= ConstantesApp.UmbralRamAlta
                            ? ConstantesApp.ColorRojo
                            : ConstantesApp.ColorTextoSuave;
                    }
                    tieneUltimaRam = true;
                    ultimaRamTiene = ram.TieneDatos;
                    ultimaRamPct = ram.Porcentaje;
                    ultimaRamUsada = ram.UsadaGB;
                    ultimaRamTotal = ram.TotalGB;
                }

                if (!tieneUltimaBateria || bateria.Porcentaje != ultimaBatPct || bateria.EstaCargando != ultimaBatCargando || bateria.TieneBateria != ultimaBatTiene)
                {
                    if (!bateria.TieneBateria)
                    {
                        PonTexto(etiquetaBateria, ConstantesApp.TextoSinBateria);
                        etiquetaBateria.ForeColor = ConstantesApp.ColorTextoSuave;
                    }
                    else if (bateria.EstaCargando)
                    {
                        PonTexto(etiquetaBateria, string.Format("{0}% - {1}", bateria.Porcentaje, ConstantesApp.TextoCargando));
                        etiquetaBateria.ForeColor = ConstantesApp.ColorVerde;
                    }
                    else
                    {
                        PonTexto(etiquetaBateria, string.Format("{0}% - {1}", bateria.Porcentaje, ConstantesApp.TextoBateria));
                        etiquetaBateria.ForeColor = bateria.Porcentaje <= ConstantesApp.UmbralBateriaBaja
                            ? ConstantesApp.ColorRojo
                            : ConstantesApp.ColorTextoSuave;
                    }
                    tieneUltimaBateria = true;
                    ultimaBatPct = bateria.Porcentaje;
                    ultimaBatCargando = bateria.EstaCargando;
                    ultimaBatTiene = bateria.TieneBateria;
                }
            }

            // Tooltip de bandeja con resumen (máx 63 caracteres en NotifyIcon).
            // Solo se toca 1 de cada 3 ticks (~3s) y solo si cambió: cada
            // asignación es una llamada al shell. Con F1 en vez de F2 cambia
            // menos (antes cambiaba casi cada segundo por los decimales).
            if ((contadorTicks % ConstantesApp.CadaCuantosTicksTooltip) == 1)
            {
            try
            {
                string textoRam = ram.TieneDatos
                    ? string.Format("RAM {0:F0}%", ram.Porcentaje)
                    : "RAM --";
                string textoBandeja = string.Format("{0:F1} GHz | CPU {1:F0}% | {2}", ghz, usoCpu, textoRam);
                if (textoBandeja.Length > ConstantesApp.LongitudMaximaTooltip)
                {
                    textoBandeja = textoBandeja.Substring(0, ConstantesApp.LongitudMaximaTooltip);
                }
                if (textoBandeja != ultimoTextoBandeja)
                {
                    iconoBandeja.Text = textoBandeja;
                    ultimoTextoBandeja = textoBandeja;
                }
            }
            catch
            {
                // El tooltip no es crítico: si falla, se ignora.
            }
            }
        }
        catch (Exception)
        {
            if (!oculta)
            {
                PonTexto(etiquetaCpu, "-- GHz");
                PonTexto(etiquetaUsoCpu, "CPU: --");
                PonTexto(etiquetaRam, "RAM: --");
                PonTexto(etiquetaBateria, "Error de lectura");
            }
        }
    }

    // Asignar Label.Text siempre redibuja aunque el texto sea igual.
    // Este guard evita layout + GDI+ cuando nada cambió (caso común: % estable).
    private static void PonTexto(Label etiqueta, string texto)
    {
        if (etiqueta.Text != texto)
        {
            etiqueta.Text = texto;
        }
    }

    private void AlCambiarArranque(object sender, EventArgs e)
    {
        ToolStripMenuItem opcion = (ToolStripMenuItem)sender;
        if (opcion.Checked)
        {
            gestorArranque.Activar();
        }
        else
        {
            gestorArranque.Desactivar();
        }
    }

    private void AlMostrarOcultar(object sender, EventArgs e)
    {
        Visible = !Visible;
        if (Visible)
        {
            Mostrar();
        }
    }

    private void AlMinimizar(object sender, EventArgs e)
    {
        // Minimizar = ocultar a la bandeja (doble clic o "Mostrar" para volver).
        Visible = false;
    }

    private void AlMostrar(object sender, EventArgs e)
    {
        Mostrar();
    }

    private void Mostrar()
    {
        // Al volver: ritmo rápido de inmediato y repintado completo.
        Visible = true;
        temporizador.Interval = ConstantesApp.IntervaloMuestreoMs;
        grafica.Invalidate();
    }

    private void AlCerrar(object sender, EventArgs e)
    {
        // Cierre real: apaga timer, quita icono y termina el proceso.
        temporizador.Stop();
        iconoBandeja.Visible = false;
        Application.Exit();
    }

    private void AlEmpezarArrastre(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            arrastrando = true;
            puntoArrastre = e.Location;
        }
    }

    private void AlArrastrar(object sender, MouseEventArgs e)
    {
        if (arrastrando)
        {
            Left += e.X - puntoArrastre.X;
            Top += e.Y - puntoArrastre.Y;
        }
    }

    private void AlTerminarArrastre(object sender, MouseEventArgs e)
    {
        arrastrando = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            temporizador.Dispose();
            iconoBandeja.Dispose();
            menuBandeja.Dispose();
            fuenteCpu.Dispose();
            fuenteUso.Dispose();
            fuenteBateria.Dispose();
        }
        base.Dispose(disposing);
    }
}
}
