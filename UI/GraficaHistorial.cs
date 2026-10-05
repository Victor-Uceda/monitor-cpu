using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorCpu
{

// Mini-gráfica de historial. Solo dibuja, no mide nada.
// Optimizada: 0 allocations en estado estable (sin ToArray ni new por frame).
public sealed class GraficaHistorial : Control
{
    // Cola fija: al llegar al tope se saca el más viejo.
    private readonly Queue<double> historial = new Queue<double>();

    // Arreglo reutilizado para dibujar sin crear basura en cada frame.
    // Solo se redimensiona mientras crece (primeros 60s), luego nunca más.
    private PointF[] puntosParaDibujar = new PointF[0];

    private readonly Pen lapizLinea = new Pen(ConstantesApp.ColorLineaGrafica, ConstantesApp.GrosorLineaGrafica);

    public GraficaHistorial()
    {
        // Doble buffer = no parpadea y gasta menos CPU.
        DoubleBuffered = true;
        BackColor = ConstantesApp.ColorFondo;
        ForeColor = ConstantesApp.ColorTextoSuave;
    }

    public void AgregarPunto(double valor)
    {
        if (historial.Count >= ConstantesApp.CapacidadHistorial)
        {
            historial.Dequeue();
        }
        historial.Enqueue(valor);

        // Si está oculta, no se repinta: el tick ya evita llamarnos.
        if (Visible)
        {
            // Solo repinta la gráfica, no toda la ventana.
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        int cantidad = historial.Count;
        if (cantidad < 2)
        {
            return;
        }

        // Escala al rango real de los últimos segundos + margen.
        // Se itera la cola directo: sin ToArray (0 alloc).
        double maximo = double.NegativeInfinity;
        double minimo = double.PositiveInfinity;
        foreach (double valor in historial)
        {
            if (valor > maximo) maximo = valor;
            if (valor < minimo) minimo = valor;
        }
        double rango = maximo - minimo;
        if (rango < ConstantesApp.RangoMinimoGrafica)
        {
            maximo += ConstantesApp.RangoMinimoGrafica / 2;
            minimo -= ConstantesApp.RangoMinimoGrafica / 2;
        }
        else
        {
            double margen = rango * ConstantesApp.MargenGraficaFactor;
            maximo += margen;
            minimo -= margen;
        }

        // Se redimensiona el arreglo de dibujo solo cuando cambia la cantidad
        // (crece 1 vez/seg los primeros 60s, luego nunca más).
        if (puntosParaDibujar.Length != cantidad)
        {
            puntosParaDibujar = new PointF[cantidad];
        }

        // Lo más nuevo va a la derecha: la línea crece desde el borde derecho.
        int i = 0;
        foreach (double valor in historial)
        {
            float x = (float)(ConstantesApp.CapacidadHistorial - cantidad + i) / (ConstantesApp.CapacidadHistorial - 1) * Width;
            float y = Height - (float)((valor - minimo) / (maximo - minimo)) * (Height - ConstantesApp.RellenoVerticalGrafica) - ConstantesApp.BordeInferiorGrafica;
            puntosParaDibujar[i] = new PointF(x, y);
            i++;
        }

        e.Graphics.DrawLines(lapizLinea, puntosParaDibujar);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lapizLinea.Dispose();
        }
        base.Dispose(disposing);
    }
}
}
