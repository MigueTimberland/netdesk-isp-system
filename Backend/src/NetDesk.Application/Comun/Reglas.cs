namespace NetDesk.Application.Comun;

public static class Reglas
{
    public const string NombreUsuario = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,20}$";
    public const string Password = @"^(?=.*[A-Z])(?=.*\d).{8,30}$";
    public const string Identificacion = @"^\d{10,13}$";
    public const string Telefono = @"^09\d{8,13}$";
    public const string NombrePersona = @"^[A-Za-zÁÉÍÓÚÜáéíóúüÑñ ]{2,80}$";
    public const string Correo = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public const string MensajeUsuario =
        "El usuario debe tener entre 8 y 20 caracteres, solo letras y números, y al menos un número.";

    public const string MensajePassword =
        "La contraseña debe tener entre 8 y 30 caracteres, al menos una mayúscula y al menos un número.";

    public const string MensajeIdentificacion =
        "La identificación debe tener entre 10 y 13 dígitos, solo números.";

    public const string MensajeTelefono =
        "El teléfono debe empezar con 09, tener al menos 10 dígitos y solo números.";

    public const string MensajeDireccion =
        "Debe tener entre 20 y 100 caracteres.";
}

public static class Normalizador
{
    public static string Usuario(string valor) => valor.Trim();

    public static string Correo(string valor) => valor.Trim().ToLowerInvariant();

    public static string Identificacion(string valor) => valor.Trim();

    public static string Telefono(string valor) => valor.Trim();

    public static string Nombre(string valor) =>
        string.Join(' ', valor.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string Texto(string valor) => valor.Trim();
}

public static class NombresEstado
{
    public static string Usuario(Domain.Enums.EstadoUsuario estado) => estado switch
    {
        Domain.Enums.EstadoUsuario.PendienteAprobacion => "Pendiente de aprobación",
        Domain.Enums.EstadoUsuario.Activo => "Activo",
        Domain.Enums.EstadoUsuario.Inactivo => "Inactivo",
        Domain.Enums.EstadoUsuario.Bloqueado => "Bloqueado",
        _ => estado.ToString()
    };

    public static string Cliente(Domain.Enums.EstadoCliente estado) => estado switch
    {
        Domain.Enums.EstadoCliente.Activo => "Activo",
        Domain.Enums.EstadoCliente.Inactivo => "Inactivo",
        _ => estado.ToString()
    };

    public static string Turno(Domain.Enums.EstadoTurno estado) => estado switch
    {
        Domain.Enums.EstadoTurno.Asignado => "Asignado",
        Domain.Enums.EstadoTurno.EnAtencion => "En atención",
        Domain.Enums.EstadoTurno.Atendido => "Atendido",
        Domain.Enums.EstadoTurno.Cancelado => "Cancelado",
        _ => estado.ToString()
    };

    public static (string Codigo, string Nombre) Contrato(Domain.Enums.EstadoContrato estado) => estado switch
    {
        Domain.Enums.EstadoContrato.Vigente => ("VIG", "Contrato vigente"),
        Domain.Enums.EstadoContrato.Sustituido => ("SUS", "Contrato sustituido"),
        Domain.Enums.EstadoContrato.Renovacion => ("REN", "Renovación de servicio"),
        Domain.Enums.EstadoContrato.Cancelado => ("CAN", "Contrato cancelado"),
        _ => (estado.ToString(), estado.ToString())
    };

    public static Domain.Enums.EstadoUsuario ParseUsuario(string valor)
    {
        var normalizado = valor.Trim().ToLowerInvariant();
        return normalizado switch
        {
            "activo" => Domain.Enums.EstadoUsuario.Activo,
            "inactivo" => Domain.Enums.EstadoUsuario.Inactivo,
            "bloqueado" => Domain.Enums.EstadoUsuario.Bloqueado,
            _ => throw new Domain.Common.ReglaNegocioException("Estado no válido. Use Activo, Inactivo o Bloqueado.")
        };
    }

    public static Domain.Enums.EstadoCliente ParseCliente(string valor)
    {
        var normalizado = valor.Trim().ToLowerInvariant();
        return normalizado switch
        {
            "activo" => Domain.Enums.EstadoCliente.Activo,
            "inactivo" => Domain.Enums.EstadoCliente.Inactivo,
            _ => throw new Domain.Common.ReglaNegocioException("Estado no válido. Use Activo o Inactivo.")
        };
    }
}
