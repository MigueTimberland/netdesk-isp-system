using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDesk.Application.Dtos;
using NetDesk.Application.Servicios;

namespace NetDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    [HttpPost("recuperar")]
    [AllowAnonymous]
    public async Task<ActionResult<RecuperarPasswordResponse>> Recuperar(RecuperarPasswordRequest request, CancellationToken ct) =>
        Ok(await auth.RecuperarAsync(request, ct));

    [HttpPost("restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> Restablecer(RestablecerPasswordRequest request, CancellationToken ct)
    {
        await auth.RestablecerAsync(request, ct);
        return Ok(new { mensaje = "La contraseña fue actualizada." });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(ct);
        return Ok(new { mensaje = "Sesión cerrada." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioSesionDto>> Me(CancellationToken ct) =>
        Ok(await auth.YoAsync(ct));
}
