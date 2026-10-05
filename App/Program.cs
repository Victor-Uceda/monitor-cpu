using System;
using System.Threading;
using System.Windows.Forms;

namespace MonitorCpu
{

// Entrada del programa. Arma las piezas y abre la ventana.
// Con Mutex de instancia única: si ya está abierto, el segundo .exe se cierra solo.
internal static class Program
{
    private static Mutex mutexInstancia;

    [STAThread]
    private static void Main()
    {
        bool esPrimeraInstancia;
        try
        {
            mutexInstancia = new Mutex(true, ConstantesApp.NombreMutex, out esPrimeraInstancia);
        }
        catch
        {
            // Si no se puede crear el mutex (permisos), se deja arrancar igual.
            esPrimeraInstancia = true;
        }

        if (!esPrimeraInstancia)
        {
            // Ya hay una ventana abierta: no se abre otra.
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // El monitor nunca debe quitarle CPU a lo importante (juegos, etc.).
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
        }
        catch
        {
            // Sin permiso para cambiar prioridad: se sigue igual.
        }

        // Se crean a mano (sin contenedor): legible y suficiente para los sensores.
        LectorFrecuenciaCpu lectorCpu = new LectorFrecuenciaCpu();
        LectorUsoCpu lectorUsoCpu = new LectorUsoCpu();
        LectorGpu lectorGpu = new LectorGpu();
        try
        {
            ILectorBateria lectorBateria = new LectorBateria();
            ILectorRam lectorRam = new LectorRam();
            GestorArranque gestorArranque = new GestorArranque();
            Application.Run(new VentanaPrincipal(lectorCpu, lectorBateria, lectorUsoCpu, lectorGpu, lectorRam, gestorArranque));
        }
        finally
        {
            lectorCpu.Dispose();
            lectorUsoCpu.Dispose();
            lectorGpu.Dispose();
            if (mutexInstancia != null)
            {
                try
                {
                    mutexInstancia.ReleaseMutex();
                }
                catch
                {
                    // Ya liberado o no es el dueño: nada que hacer.
                }
                mutexInstancia.Dispose();
                mutexInstancia = null;
            }
        }
    }
}
}
