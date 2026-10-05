using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MonitorCpu
{

// Activa/desactiva "iniciar con Windows".
// Solo escribe en HKCU\Run: no pide admin y cuesta 0 CPU.
public sealed class GestorArranque
{
    public bool EstaActivado()
    {
        try
        {
            using (RegistryKey clave = Registry.CurrentUser.OpenSubKey(ConstantesApp.ClaveArranqueRegistro, false))
            {
                return clave != null && clave.GetValue(ConstantesApp.NombreApp) != null;
            }
        }
        catch
        {
            // Antivirus o cleaner bloqueó el registro: se asume desactivado.
            return false;
        }
    }

    public void Activar()
    {
        try
        {
            using (RegistryKey clave = Registry.CurrentUser.OpenSubKey(ConstantesApp.ClaveArranqueRegistro, true))
            {
                if (clave != null)
                {
                    // Comillas obligatorias: la ruta puede tener espacios.
                    clave.SetValue(ConstantesApp.NombreApp, "\"" + Application.ExecutablePath + "\"");
                }
            }
        }
        catch
        {
            // Sin permiso o registro bloqueado: falla en silencio, no rompe la app.
        }
    }

    public void Desactivar()
    {
        try
        {
            using (RegistryKey clave = Registry.CurrentUser.OpenSubKey(ConstantesApp.ClaveArranqueRegistro, true))
            {
                if (clave != null)
                {
                    try
                    {
                        clave.DeleteValue(ConstantesApp.NombreApp, false);
                    }
                    catch (ArgumentException)
                    {
                        // Ya estaba desactivado, no hay nada que borrar.
                    }
                }
            }
        }
        catch
        {
            // Sin permiso: se ignora, la opción simplemente no cambia.
        }
    }
}
}
