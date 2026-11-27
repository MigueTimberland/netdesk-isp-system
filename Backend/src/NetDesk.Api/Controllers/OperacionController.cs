using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDesk.Application.Dtos;
using NetDesk.Application.Servicios;

namespace NetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cajas")]
public class CajasController(CajaService cajas) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrador,Gestor,Cajero")]
    public async Task<IReadOnlyList<CajaDto>> Listar(CancellationToken ct) => await cajas.ListarAsync(ct);

    [HttpGet("mias")]
    [Authorize(Roles = "Cajero")]
    public async Task<IReadOnlyList<CajaDto>> Mias(CancellationToken ct) => await cajas.ListarMiasAsync(ct);

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<CajaDto> Crear(CrearCajaRequest request, CancellationToken ct) => await cajas.CrearAsync(request, ct);

    [HttpPost("{id:int}/asignaciones")]
    [Authorize(Roles = "Gestor")]
    public async Task<CajaDto> Asignar(int id, AsignarCajeroRequest request, CancellationToken ct) =>
        await cajas.AsignarAsync(id, request, ct);

    [HttpDelete("{id:int}/asignaciones/{asignacionId:int}")]
    [Authorize(Roles = "Gestor")]
    public async Task<CajaDto> Quitar(int id, int asignacionId, CancellationToken ct) =>
        await cajas.QuitarAsignacionAsync(id, asignacionId, ct);

    [HttpPost("{id:int}/abrir")]
    [Authorize(Roles = "Cajero")]
    public async Task<CajaDto> Abrir(int id, CancellationToken ct) => await cajas.AbrirAsync(id, ct);

    [HttpPost("{id:int}/cerrar")]
    [Authorize(Roles = "Cajero")]
    public async Task<CajaDto> Cerrar(int id, CancellationToken ct) => await cajas.CerrarAsync(id, ct);
}

[ApiController]
[Authorize]
[Route("api/turnos")]
public class TurnosController(TurnoService turnos) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrador,Gestor,Cajero")]
    public async Task<IReadOnlyList<TurnoDto>> Listar([FromQuery] int? cajaId, CancellationToken ct) =>
        await turnos.ListarHoyAsync(cajaId, ct);

    [HttpGet("mios")]
    [Authorize(Roles = "Cajero")]
    public async Task<IReadOnlyList<TurnoDto>> Mios(CancellationToken ct) => await turnos.ListarMiosAsync(ct);

    [HttpPost]
    [Authorize(Roles = "Gestor")]
    public async Task<IReadOnlyList<TurnoDto>> Crear(CrearTurnosRequest request, CancellationToken ct) =>
        await turnos.CrearAsync(request, ct);

    [HttpPost("{id:int}/iniciar")]
    [Authorize(Roles = "Cajero")]
    public async Task<TurnoDto> Iniciar(int id, IniciarTurnoRequest request, CancellationToken ct) =>
        await turnos.IniciarAsync(id, request, ct);

    [HttpPost("{id:int}/finalizar")]
    [Authorize(Roles = "Cajero")]
    public async Task<TurnoDto> Finalizar(int id, CancellationToken ct) => await turnos.FinalizarAsync(id, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Gestor")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
    {
        await turnos.CancelarAsync(id, ct);
        return Ok(new { mensaje = "Turno cancelado." });
    }
}

[ApiController]
[Authorize(Roles = "Cajero")]
[Route("api/contratos")]
public class ContratosController(ContratoService contratos) : ControllerBase
{
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<IReadOnlyList<ContratoDto>> PorCliente(int clienteId, CancellationToken ct) =>
        await contratos.ListarPorClienteAsync(clienteId, ct);

    [HttpPost]
    public async Task<ContratoDto> Contratar(ContratarRequest request, CancellationToken ct) =>
        await contratos.ContratarAsync(request, ct);

    [HttpPost("{id:int}/cambiar-servicio")]
    public async Task<ContratoDto> CambiarServicio(int id, CambiarServicioRequest request, CancellationToken ct) =>
        await contratos.CambiarServicioAsync(id, request, ct);

    [HttpPost("{id:int}/cambiar-forma-pago")]
    public async Task<ContratoDto> CambiarFormaPago(int id, CambiarFormaPagoRequest request, CancellationToken ct) =>
        await contratos.CambiarFormaPagoAsync(id, request, ct);

    [HttpPost("{id:int}/cancelar")]
    public async Task<ContratoDto> Cancelar(int id, CancelarContratoRequest request, CancellationToken ct) =>
        await contratos.CancelarAsync(id, request, ct);
}

[ApiController]
[Authorize(Roles = "Cajero")]
[Route("api/pagos")]
public class PagosController(ContratoService contratos) : ControllerBase
{
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<IReadOnlyList<PagoDto>> PorCliente(int clienteId, CancellationToken ct) =>
        await contratos.ListarPagosAsync(clienteId, ct);

    [HttpPost]
    public async Task<PagoDto> Registrar(RegistrarPagoRequest request, CancellationToken ct) =>
        await contratos.RegistrarPagoAsync(request, ct);
}
