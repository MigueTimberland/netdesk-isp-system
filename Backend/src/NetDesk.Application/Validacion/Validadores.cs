using FluentValidation;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;

namespace NetDesk.Application.Validacion;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("Ingrese el nombre de usuario.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingrese la contraseña.");
    }
}

public class RecuperarPasswordRequestValidator : AbstractValidator<RecuperarPasswordRequest>
{
    public RecuperarPasswordRequestValidator()
    {
        RuleFor(x => x.Correo).NotEmpty().Matches(Reglas.Correo).WithMessage("Ingrese un correo válido.");
    }
}

public class RestablecerPasswordRequestValidator : AbstractValidator<RestablecerPasswordRequest>
{
    public RestablecerPasswordRequestValidator()
    {
        RuleFor(x => x.Correo).NotEmpty().Matches(Reglas.Correo).WithMessage("Ingrese un correo válido.");
        RuleFor(x => x.Codigo).NotEmpty().Length(8).WithMessage("El código debe tener 8 caracteres.");
        RuleFor(x => x.NuevaPassword).NotEmpty().Matches(Reglas.Password).WithMessage(Reglas.MensajePassword);
    }
}

public class CrearUsuarioRequestValidator : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioRequestValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().Matches(Reglas.NombreUsuario).WithMessage(Reglas.MensajeUsuario);
        RuleFor(x => x.Password).NotEmpty().Matches(Reglas.Password).WithMessage(Reglas.MensajePassword);
        RuleFor(x => x.Nombres).NotEmpty().Matches(Reglas.NombrePersona).WithMessage("Ingrese nombres válidos, solo letras.");
        RuleFor(x => x.Apellidos).NotEmpty().Matches(Reglas.NombrePersona).WithMessage("Ingrese apellidos válidos, solo letras.");
        RuleFor(x => x.Identificacion).NotEmpty().Matches(Reglas.Identificacion).WithMessage(Reglas.MensajeIdentificacion);
        RuleFor(x => x.Correo).NotEmpty().Matches(Reglas.Correo).WithMessage("Ingrese un correo válido.");
        RuleFor(x => x.Telefono).NotEmpty().Matches(Reglas.Telefono).WithMessage(Reglas.MensajeTelefono);
        RuleFor(x => x.RolId).GreaterThan(0).WithMessage("Seleccione un rol.");
    }
}

public class ActualizarUsuarioRequestValidator : AbstractValidator<ActualizarUsuarioRequest>
{
    public ActualizarUsuarioRequestValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().Matches(Reglas.NombreUsuario).WithMessage(Reglas.MensajeUsuario);
        When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
        {
            RuleFor(x => x.Password!).Matches(Reglas.Password).WithMessage(Reglas.MensajePassword);
        });
        RuleFor(x => x.Nombres).NotEmpty().Matches(Reglas.NombrePersona).WithMessage("Ingrese nombres válidos, solo letras.");
        RuleFor(x => x.Apellidos).NotEmpty().Matches(Reglas.NombrePersona).WithMessage("Ingrese apellidos válidos, solo letras.");
        RuleFor(x => x.Identificacion).NotEmpty().Matches(Reglas.Identificacion).WithMessage(Reglas.MensajeIdentificacion);
        RuleFor(x => x.Correo).NotEmpty().Matches(Reglas.Correo).WithMessage("Ingrese un correo válido.");
        RuleFor(x => x.Telefono).NotEmpty().Matches(Reglas.Telefono).WithMessage(Reglas.MensajeTelefono);
        RuleFor(x => x.RolId).GreaterThan(0);
    }
}

public class GuardarClienteRequestValidator : AbstractValidator<GuardarClienteRequest>
{
    public GuardarClienteRequestValidator()
    {
        RuleFor(x => x.Nombres).NotEmpty().Matches(Reglas.NombrePersona).WithMessage("Ingrese nombres válidos, solo letras.");
        RuleFor(x => x.Apellidos).NotEmpty().Matches(Reglas.NombrePersona).WithMessage("Ingrese apellidos válidos, solo letras.");
        RuleFor(x => x.Identificacion).NotEmpty().Matches(Reglas.Identificacion).WithMessage(Reglas.MensajeIdentificacion);
        RuleFor(x => x.Correo).NotEmpty().Matches(Reglas.Correo).WithMessage("Ingrese un correo válido.");
        RuleFor(x => x.Telefono).NotEmpty().Matches(Reglas.Telefono).WithMessage(Reglas.MensajeTelefono);
        RuleFor(x => x.Direccion).NotEmpty().Must(v => v.Trim().Length is >= 20 and <= 100).WithMessage("La dirección " + Reglas.MensajeDireccion.ToLowerInvariant());
        RuleFor(x => x.ReferenciaDireccion).NotEmpty().Must(v => v.Trim().Length is >= 20 and <= 100).WithMessage("La referencia " + Reglas.MensajeDireccion.ToLowerInvariant());
    }
}

public class CrearCajaRequestValidator : AbstractValidator<CrearCajaRequest>
{
    public CrearCajaRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(10).Matches(@"^[A-Za-z0-9\-]+$").WithMessage("El código de caja es obligatorio.");
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Descripcion).MaximumLength(200);
    }
}

public class AsignarCajeroRequestValidator : AbstractValidator<AsignarCajeroRequest>
{
    public AsignarCajeroRequestValidator()
    {
        RuleFor(x => x.CajeroId).GreaterThan(0).WithMessage("Seleccione un cajero.");
    }
}

public class CrearTurnosRequestValidator : AbstractValidator<CrearTurnosRequest>
{
    public CrearTurnosRequestValidator()
    {
        RuleFor(x => x.CajaId).GreaterThan(0);
        RuleFor(x => x.CajeroId).GreaterThan(0);
        RuleFor(x => x.TipoAtencionId).GreaterThan(0);
        RuleFor(x => x.Cantidad).InclusiveBetween(1, 20).WithMessage("Puede asignar entre 1 y 20 turnos.");
    }
}

public class GuardarServicioRequestValidator : AbstractValidator<GuardarServicioRequest>
{
    public GuardarServicioRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Descripcion).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Equipos).NotEmpty().MaximumLength(200);
        RuleFor(x => x.VelocidadMbps).InclusiveBetween(1, 10000);
        RuleFor(x => x.Precio).GreaterThan(0);
    }
}

public class ContratarRequestValidator : AbstractValidator<ContratarRequest>
{
    public ContratarRequestValidator()
    {
        RuleFor(x => x.ClienteId).GreaterThan(0);
        RuleFor(x => x.ServicioId).GreaterThan(0);
        RuleFor(x => x.FormaPagoId).GreaterThan(0);
        RuleFor(x => x.TurnoId).GreaterThan(0);
    }
}

public class CambiarServicioRequestValidator : AbstractValidator<CambiarServicioRequest>
{
    public CambiarServicioRequestValidator()
    {
        RuleFor(x => x.ServicioId).GreaterThan(0);
        RuleFor(x => x.TurnoId).GreaterThan(0);
    }
}

public class CambiarFormaPagoRequestValidator : AbstractValidator<CambiarFormaPagoRequest>
{
    public CambiarFormaPagoRequestValidator()
    {
        RuleFor(x => x.FormaPagoId).GreaterThan(0);
        RuleFor(x => x.TurnoId).GreaterThan(0);
    }
}

public class CancelarContratoRequestValidator : AbstractValidator<CancelarContratoRequest>
{
    public CancelarContratoRequestValidator()
    {
        RuleFor(x => x.TurnoId).GreaterThan(0);
        RuleFor(x => x.Observacion).MaximumLength(300);
    }
}

public class RegistrarPagoRequestValidator : AbstractValidator<RegistrarPagoRequest>
{
    public RegistrarPagoRequestValidator()
    {
        RuleFor(x => x.ContratoId).GreaterThan(0);
        RuleFor(x => x.TurnoId).GreaterThan(0);
        RuleFor(x => x.Referencia).MaximumLength(80);
    }
}

public class CambiarEstadoRequestValidator : AbstractValidator<CambiarEstadoRequest>
{
    public CambiarEstadoRequestValidator()
    {
        RuleFor(x => x.Estado).NotEmpty();
    }
}
