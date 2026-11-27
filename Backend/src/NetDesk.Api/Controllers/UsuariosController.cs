using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDesk.Application.Dtos;
using NetDesk.Application.Servicios;

namespace NetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController(UsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IReadOnlyList<UsuarioDto>> Listar(CancellationToken ct) => await usuarios.ListarAsync(ct);

    [HttpGet("pendientes")]
    [Authorize(Roles = "Administrador,Gestor")]
    public async Task<IReadOnlyList<UsuarioDto>> Pendientes(CancellationToken ct) => await usuarios.ListarPendientesAsync(ct);

    [HttpGet("por-rol/{rol}")]
    [Authorize(Roles = "Administrador,Gestor")]
    public async Task<IReadOnlyList<UsuarioDto>> PorRol(string rol, CancellationToken ct) =>
        await usuarios.ListarActivosPorRolAsync(rol, ct);

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<UsuarioDto> Obtener(int id, CancellationToken ct) => await usuarios.ObtenerAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "Administrador,Gestor")]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var creado = await usuarios.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<UsuarioDto> Actualizar(int id, ActualizarUsuarioRequest request, CancellationToken ct) =>
        await usuarios.ActualizarAsync(id, request, ct);

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<UsuarioDto> Estado(int id, CambiarEstadoRequest request, CancellationToken ct) =>
        await usuarios.CambiarEstadoAsync(id, request, ct);

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = "Administrador")]
    public async Task<UsuarioDto> Aprobar(int id, CancellationToken ct) => await usuarios.AprobarAsync(id, ct);

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = "Administrador")]
    public async Task<UsuarioDto> Rechazar(int id, CancellationToken ct) => await usuarios.RechazarAsync(id, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await usuarios.EliminarAsync(id, ct);
        return Ok(new { mensaje = "Usuario eliminado." });
    }

    [HttpGet("plantilla")]
    [Authorize(Roles = "Administrador")]
    public IActionResult Plantilla() =>
        File(usuarios.Plantilla(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-usuarios.xlsx");

    [HttpPost("carga-masiva")]
    [Authorize(Roles = "Administrador")]
    [RequestSizeLimit(10_000_000)]
    public async Task<CargaMasivaResultadoDto> Cargar(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            throw new NetDesk.Domain.Common.ReglaNegocioException("Seleccione un archivo .xlsx o .csv.");
        }

        await using var stream = archivo.OpenReadStream();
        return await usuarios.CargarMasivoAsync(stream, archivo.FileName, ct);
    }
}
