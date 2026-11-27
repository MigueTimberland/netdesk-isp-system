using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDesk.Application.Dtos;
using NetDesk.Application.Servicios;

namespace NetDesk.Api.Controllers;

[ApiController]
[Authorize(Roles = "Cajero")]
[Route("api/clientes")]
public class ClientesController(ClienteService clientes) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ClienteDto>> Listar([FromQuery] string? texto, CancellationToken ct) =>
        await clientes.ListarAsync(texto, ct);

    [HttpGet("{id:int}")]
    public async Task<ClienteDto> Obtener(int id, CancellationToken ct) => await clientes.ObtenerAsync(id, ct);

    [HttpGet("por-identificacion/{identificacion}")]
    public async Task<ActionResult<ClienteDto>> PorIdentificacion(string identificacion, CancellationToken ct)
    {
        var cliente = await clientes.BuscarPorIdentificacionAsync(identificacion, ct);
        return cliente is null ? NotFound(new { mensaje = "No existe un cliente con esa identificación." }) : Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Crear(GuardarClienteRequest request, CancellationToken ct)
    {
        var creado = await clientes.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}")]
    public async Task<ClienteDto> Actualizar(int id, GuardarClienteRequest request, CancellationToken ct) =>
        await clientes.ActualizarAsync(id, request, ct);

    [HttpPatch("{id:int}/estado")]
    public async Task<ClienteDto> Estado(int id, CambiarEstadoRequest request, CancellationToken ct) =>
        await clientes.CambiarEstadoAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await clientes.EliminarAsync(id, ct);
        return Ok(new { mensaje = "Cliente eliminado." });
    }

    [HttpGet("plantilla")]
    public IActionResult Plantilla() =>
        File(clientes.Plantilla(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-clientes.xlsx");

    [HttpPost("carga-masiva")]
    [RequestSizeLimit(10_000_000)]
    public async Task<CargaMasivaResultadoDto> Cargar(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
        {
            throw new NetDesk.Domain.Common.ReglaNegocioException("Seleccione un archivo .xlsx o .csv.");
        }

        await using var stream = archivo.OpenReadStream();
        return await clientes.CargarMasivoAsync(stream, archivo.FileName, ct);
    }
}
