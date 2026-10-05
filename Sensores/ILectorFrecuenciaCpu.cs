using System;

namespace MonitorCpu
{

// Contrato para leer GHz. Si mañana cambia la forma de medir,
// solo se crea otra clase con este mismo contrato.
public interface ILectorFrecuenciaCpu : IDisposable
{
    double LeerGHz();
}
}
