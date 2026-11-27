using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDesk.Application.Dtos;
using NetDesk.Application.Servicios;

namespace NetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/catalogos")]
public class CatalogosController(CatalogoService catalogos) : ControllerBase
{
    [HttpGet("roles")]
    [Authorize(Roles = "Administrador,Gestor")]
    public async Task<IReadOnlyList<CatalogoDto>> Roles(CancellationToken ct) => await catalogos.RolesOperativosAsync(ct);

    [HttpGet("tipos-atencion")]
    [Authorize(Roles = "Administrador,Gestor,Cajero")]
    public async Task<IReadOnlyList<CatalogoDto>> Tipos(CancellationToken ct) => await catalogos.TiposAtencionAsync(ct);

    [HttpGet("formas-pago")]
    public async Task<IReadOnlyList<CatalogoDto>> Formas(CancellationToken ct) => await catalogos.FormasPagoAsync(ct);
}

[ApiController]
[Authorize]
[Route("api/servicios")]
public class ServiciosController(CatalogoService catalogos) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ServicioDto>> Listar([FromQuery] bool soloActivos = false, CancellationToken ct = default) =>
        await catalogos.ServiciosAsync(soloActivos, ct);

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ServicioDto> Crear(GuardarServicioRequest request, CancellationToken ct) =>
        await catalogos.CrearServicioAsync(request, ct);

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ServicioDto> Actualizar(int id, GuardarServicioRequest request, CancellationToken ct) =>
        await catalogos.ActualizarServicioAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await catalogos.EliminarServicioAsync(id, ct);
        return Ok(new { mensaje = "Servicio eliminado." });
    }
}

[ApiController]
[Authorize]
[Route("api/menus")]
public class MenusController(CatalogoService catalogos) : ControllerBase
{
    [HttpGet("mios")]
    public async Task<IReadOnlyList<MenuDto>> Mios(CancellationToken ct) => await catalogos.MisMenusAsync(ct);
}

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet("bienvenida")]
    public async Task<BienvenidaDto> Bienvenida(CancellationToken ct) => await dashboard.BienvenidaAsync(ct);

    [HttpGet("indicadores")]
    [Authorize(Roles = "Administrador")]
    public async Task<DashboardDto> Indicadores([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        await dashboard.IndicadoresAsync(desde, hasta, ct);
}

[ApiController]
[Route("api/salud")]
public class SaludController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { estado = "ok", servicio = "NetDesk ISP" });
}
