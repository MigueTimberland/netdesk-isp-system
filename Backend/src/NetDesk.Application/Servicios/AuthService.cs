using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Application.Servicios;

public sealed class AuthService(
    IUsuarioRepositorio usuarios,
    ITokenRecuperacionRepositorio tokens,
    IPasswordService passwords,
    ITokenJwtService jwt,
    ICifradoService cifrado,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    IOpcionesAplicacion opciones,
    Mapeador mapeador,
    IValidator<LoginRequest> loginValidator,
    IValidator<RecuperarPasswordRequest> recuperarValidator,
    IValidator<RestablecerPasswordRequest> restablecerValidator,
    ILogger<AuthService> logger)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidarAsync(request, ct);
        var nombre = Normalizador.Usuario(request.NombreUsuario);
        var usuario = await usuarios.ObtenerPorNombreAsync(nombre, ct);

        if (usuario is null)
        {
            logger.LogWarning("Alerta de inicio de sesión fallido para {NombreUsuario}.", nombre);
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        if (!passwords.Verificar(usuario.PasswordHash, request.Password))
        {
            if (usuario.Estado != EstadoUsuario.Bloqueado)
            {
                usuario.RegistrarIntentoFallido();
                await unidad.GuardarAsync(ct);
            }

            if (usuario.Estado == EstadoUsuario.Bloqueado)
            {
                logger.LogWarning("Alerta: usuario {NombreUsuario} bloqueado por intentos fallidos.", usuario.NombreUsuario);
                throw new ReglaNegocioException("El usuario está bloqueado. Contacte al administrador.");
            }

            logger.LogWarning("Alerta de inicio de sesión fallido para {NombreUsuario}.", usuario.NombreUsuario);
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        usuario.RegistrarInicioSesion();
        await unidad.GuardarAsync(ct);
        var (token, expira) = jwt.Emitir(usuario);
        logger.LogInformation("Inicio de sesión de {NombreUsuario} con rol {Rol}.", usuario.NombreUsuario, usuario.Rol.Nombre);
        return new LoginResponse(token, expira, "JWE", mapeador.Sesion(usuario));
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(actual.Id, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");
        usuario.CerrarSesion();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Cierre de sesión de {NombreUsuario}.", usuario.NombreUsuario);
    }

    public async Task<UsuarioSesionDto> YoAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(actual.Id, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");
        return mapeador.Sesion(usuario);
    }

    public async Task<RecuperarPasswordResponse> RecuperarAsync(RecuperarPasswordRequest request, CancellationToken ct)
    {
        await recuperarValidator.ValidarAsync(request, ct);
        const string mensaje = "Si el correo está registrado, se generó un código de recuperación vigente por 30 minutos.";
        var indice = cifrado.Indice(Normalizador.Correo(request.Correo));
        var usuario = await usuarios.ObtenerPorCorreoIndiceAsync(indice, ct);
        if (usuario is null || usuario.Estado is EstadoUsuario.Inactivo)
        {
            logger.LogWarning("Alerta: solicitud de recuperación para un correo no operable.");
            return new RecuperarPasswordResponse(mensaje, null);
        }

        var codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        await tokens.InvalidarPendientesAsync(usuario.Id, ct);
        await tokens.AgregarAsync(new TokenRecuperacion
        {
            UsuarioId = usuario.Id,
            TokenHash = HashCodigo(codigo),
            ExpiraEn = DateTime.UtcNow.AddMinutes(30)
        }, ct);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de código de recuperación para {NombreUsuario}.", usuario.NombreUsuario);
        return new RecuperarPasswordResponse(mensaje, opciones.ExponerCodigoRecuperacion ? codigo : null);
    }

    public async Task RestablecerAsync(RestablecerPasswordRequest request, CancellationToken ct)
    {
        await restablecerValidator.ValidarAsync(request, ct);
        var indice = cifrado.Indice(Normalizador.Correo(request.Correo));
        var usuario = await usuarios.ObtenerPorCorreoIndiceAsync(indice, ct)
            ?? throw new ReglaNegocioException("El código de recuperación no es válido.");

        var token = await tokens.ObtenerVigentePorHashAsync(HashCodigo(request.Codigo), ct);
        if (token is null || token.UsuarioId != usuario.Id)
        {
            logger.LogWarning("Alerta: código de recuperación inválido para {NombreUsuario}.", usuario.NombreUsuario);
            throw new ReglaNegocioException("El código de recuperación no es válido o expiró.");
        }

        usuario.ActualizarPassword(passwords.Hash(request.NuevaPassword));
        token.MarcarUsado();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de contraseña de {NombreUsuario}.", usuario.NombreUsuario);
    }

    private static string HashCodigo(string codigo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codigo.Trim().ToUpperInvariant()));
        return Convert.ToHexString(bytes);
    }
}
