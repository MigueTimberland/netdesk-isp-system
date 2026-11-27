namespace NetDesk.Domain.Enums;

public enum EstadoUsuario
{
    PendienteAprobacion = 0,
    Activo = 1,
    Inactivo = 2,
    Bloqueado = 3
}

public enum EstadoCliente
{
    Activo = 1,
    Inactivo = 2
}

public enum EstadoCaja
{
    Activa = 1,
    Inactiva = 2
}

public enum EstadoTurno
{
    Asignado = 0,
    EnAtencion = 1,
    Atendido = 2,
    Cancelado = 3
}

public enum EstadoContrato
{
    Vigente = 0,
    Sustituido = 1,
    Renovacion = 2,
    Cancelado = 3
}
