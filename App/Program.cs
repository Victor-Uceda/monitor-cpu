using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace MonitorCpu
{

// Entrada del programa. Arma las piezas y abre la ventana.
// Con doble barrera anti-duplicados:
//  1) Mutex Global (todas las sesiones) con fallback a Local.
//  2) Chequeo por nombre+ ruta de proceso (cubre mutex sin permisos).
// Si ya hay una instancia, el segundo .exe se cierra solo.
internal static class Program
{
    private static Mutex mutexInstancia;

    [STAThread]
    private static void Main()
    {
        if (YaHayOtraInstancia())
        {
            // Ya hay una ventana abierta: no se abre otra.
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // El monitor nunca debe quitarle CPU a lo importante (juegos, etc.).
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch
        {
            // Sin permiso para cambiar prioridad: se sigue igual.
        }

        // Se crean a mano (sin contenedor): legible y suficiente para los sensores.
        // Opcion A: los constructores son ligeros (~0ms, init pesado en fondo),
        // asi la ventana pinta al instante sin esperar a WMI.
        LectorFrecuenciaCpu lectorCpu = new LectorFrecuenciaCpu();
        LectorUsoCpu lectorUsoCpu = new LectorUsoCpu();
        try
        {
            ILectorBateria lectorBateria = new LectorBateria();
            ILectorRam lectorRam = new LectorRam();
            GestorArranque gestorArranque = new GestorArranque();
            Application.Run(new VentanaPrincipal(lectorCpu, lectorBateria, lectorUsoCpu, lectorRam, gestorArranque));
        }
        finally
        {
            lectorCpu.Dispose();
            lectorUsoCpu.Dispose();
            LiberarMutex();
        }
    }

    private static bool YaHayOtraInstancia()
    {
        // Barrera 1: Mutex. Global cubre todas las sesiones (RDP, etc.);
        // si no hay permiso para Global, se usa Local (misma sesión).
        bool duplicado;
        if (IntentarAdquirirMutex(@"Global\" + ConstantesApp.NombreMutex, out mutexInstancia, out duplicado))
        {
            return false;
        }
        if (duplicado)
        {
            // El mutex ya existía: otra instancia está viva.
            LiberarMutex();
            return true;
        }
        if (IntentarAdquirirMutex(@"Local\" + ConstantesApp.NombreMutex, out mutexInstancia, out duplicado))
        {
            return false;
        }
        if (duplicado)
        {
            LiberarMutex();
            return true;
        }

        // Barrera 2: el mutex no se pudo crear (permisos). Se compara por
        // proceso del mismo .exe: si hay otro vivo, no se abre ventana.
        return HayOtroProcesoIgual();
    }

    // Devuelve true si adquirió el mutex (única instancia). Si devuelve
    // false, 'duplicado' dice si hay otro vivo (true) o si hubo error y
    // hay que usar el plan B (false).
    private static bool IntentarAdquirirMutex(string nombre, out Mutex mutex, out bool duplicado)
    {
        mutex = null;
        duplicado = false;
        try
        {
            bool creada;
            Mutex candidato = new Mutex(true, nombre, out creada);
            if (creada)
            {
                mutex = candidato;
                return true;
            }
            // Ya existía: otra instancia está viva. Se suelta la referencia.
            try
            {
                candidato.Dispose();
            }
            catch
            {
            }
            duplicado = true;
            return false;
        }
        catch (AbandonedMutexException)
        {
            // El anterior .exe murió sin liberar: ahora somos la única
            // instancia viva. Se abre el mutex existente para custodiarlo.
            try
            {
                mutex = Mutex.OpenExisting(nombre);
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Sin permiso para este namespace: el llamador prueba el siguiente.
            return false;
        }
        catch
        {
            // Cualquier otro error: sin mutex, el llamador usa plan B (procesos).
            return false;
        }
    }

    private static bool HayOtroProcesoIgual()
    {
        try
        {
            Process actual = Process.GetCurrentProcess();
            Process[] candidatos = Process.GetProcessesByName(actual.ProcessName);
            string rutaActual = RutaProceso(actual);
            foreach (Process p in candidatos)
            {
                try
                {
                    if (p.Id == actual.Id)
                    {
                        continue;
                    }
                    // Mismo nombre pero otro .exe (otra carpeta): no cuenta.
                    // Si no se puede leer la ruta, se asume duplicado (seguro).
                    string ruta = RutaProceso(p);
                    if (rutaActual == null || ruta == null)
                    {
                        return true;
                    }
                    if (string.Equals(ruta, rutaActual, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }
                finally
                {
                    try
                    {
                        p.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string RutaProceso(Process p)
    {
        try
        {
            return p.MainModule.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static void LiberarMutex()
    {
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
            try
            {
                mutexInstancia.Dispose();
            }
            catch
            {
            }
            mutexInstancia = null;
        }
    }
}
}
