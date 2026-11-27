using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Application.Servicios;

public sealed class ContratoService(
    IContratoRepositorio contratos,
    IPagoRepositorio pagos,
    IClienteRepositorio clientes,
    ITurnoRepositorio turnos,
    ICajaRepositorio cajas,
    ICatalogoRepositorio catalogos,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    IReloj reloj,
    Mapeador mapeador,
    IValidator<ContratarRequest> contratarValidator,
    IValidator<CambiarServicioRequest> cambiarServicioValidator,
    IValidator<CambiarFormaPagoRequest> cambiarPagoValidator,
    IValidator<CancelarContratoRequest> cancelarValidator,
    IValidator<RegistrarPagoRequest> pagoValidator,
    ILogger<ContratoService> logger)
{
    public async Task<IReadOnlyList<ContratoDto>> ListarPorClienteAsync(int clienteId, CancellationToken ct)
    {
        var lista = await contratos.ListarPorClienteAsync(clienteId, ct);
        return lista.Select(mapeador.Contrato).ToList();
    }

    public async Task<IReadOnlyList<PagoDto>> ListarPagosAsync(int clienteId, CancellationToken ct)
    {
        var lista = await pagos.ListarPorClienteAsync(clienteId, ct);
        return lista.Select(mapeador.Pago).ToList();
    }

    public async Task<ContratoDto> ContratarAsync(ContratarRequest request, CancellationToken ct)
    {
        await contratarValidator.ValidarAsync(request, ct);
        var turno = await TurnoDeAsync(request.TurnoId, "AC", request.ClienteId, ct);
        var cliente = await ClienteActivoAsync(request.ClienteId, ct);
        if (await contratos.ObtenerOperablePorClienteAsync(cliente.Id, ct) is not null)
        {
            throw new ReglaNegocioException("El cliente ya tiene un contrato vigente. Use cambio de servicio o cancele el actual.");
        }

        var servicio = await ServicioActivoAsync(request.ServicioId, ct);
        var forma = await FormaActivaAsync(request.FormaPagoId, ct);
        var numero = await contratos.SiguienteNumeroAsync(ct);
        var contrato = Contrato.Nuevo(cliente.Id, servicio.Id, forma.Id, numero, actual.Id, turno.Id, reloj.UtcAhora, 12);
        await contratos.AgregarAsync(contrato, ct);
        turno.MarcarProceso();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de contrato {Numero} para cliente {ClienteId} por {Actor}.", contrato.Numero, cliente.Id, actual.NombreUsuario);
        return mapeador.Contrato(await contratos.ObtenerPorIdAsync(contrato.Id, ct) ?? contrato);
    }

    public async Task<ContratoDto> CambiarServicioAsync(int contratoId, CambiarServicioRequest request, CancellationToken ct)
    {
        await cambiarServicioValidator.ValidarAsync(request, ct);
        var actualContrato = await contratos.ObtenerPorIdAsync(contratoId, ct)
            ?? throw new NoEncontradoException("Contrato no encontrado.");
        var turno = await TurnoDeAsync(request.TurnoId, "CS", actualContrato.ClienteId, ct);
        var nuevo = await ServicioActivoAsync(request.ServicioId, ct);
        if (nuevo.VelocidadMbps == actualContrato.Servicio.VelocidadMbps)
        {
            throw new ReglaNegocioException("El cambio debe ser a un servicio de mayor o menor velocidad.");
        }

        var numero = await contratos.SiguienteNumeroAsync(ct);
        var renovacion = actualContrato.Sustituir(nuevo.Id, numero, actual.Id, turno.Id);
        await contratos.AgregarAsync(renovacion, ct);
        turno.MarcarProceso();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de contrato {Origen} a estado SUS. Creación de renovación {Numero} por {Actor}.", actualContrato.Numero, renovacion.Numero, actual.NombreUsuario);
        return mapeador.Contrato(await contratos.ObtenerPorIdAsync(renovacion.Id, ct) ?? renovacion);
    }

    public async Task<ContratoDto> CambiarFormaPagoAsync(int contratoId, CambiarFormaPagoRequest request, CancellationToken ct)
    {
        await cambiarPagoValidator.ValidarAsync(request, ct);
        var contrato = await contratos.ObtenerPorIdAsync(contratoId, ct)
            ?? throw new NoEncontradoException("Contrato no encontrado.");
        var turno = await TurnoDeAsync(request.TurnoId, "CP", contrato.ClienteId, ct);
        _ = await FormaActivaAsync(request.FormaPagoId, ct);
        contrato.CambiarFormaPago(request.FormaPagoId);
        turno.MarcarProceso();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de forma de pago del contrato {Numero} por {Actor}.", contrato.Numero, actual.NombreUsuario);
        return mapeador.Contrato(await contratos.ObtenerPorIdAsync(contrato.Id, ct) ?? contrato);
    }

    public async Task<ContratoDto> CancelarAsync(int contratoId, CancelarContratoRequest request, CancellationToken ct)
    {
        await cancelarValidator.ValidarAsync(request, ct);
        var contrato = await contratos.ObtenerPorIdAsync(contratoId, ct)
            ?? throw new NoEncontradoException("Contrato no encontrado.");
        var turno = await TurnoDeAsync(request.TurnoId, "CC", contrato.ClienteId, ct);
        contrato.Cancelar(string.IsNullOrWhiteSpace(request.Observacion) ? "Cancelación solicitada en caja." : request.Observacion.Trim());
        turno.MarcarProceso();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización: contrato {Numero} cancelado por {Actor}. Fecha fin {FechaFin}.", contrato.Numero, actual.NombreUsuario, contrato.FechaFin);
        return mapeador.Contrato(contrato);
    }

    public async Task<PagoDto> RegistrarPagoAsync(RegistrarPagoRequest request, CancellationToken ct)
    {
        await pagoValidator.ValidarAsync(request, ct);
        var contrato = await contratos.ObtenerPorIdAsync(request.ContratoId, ct)
            ?? throw new NoEncontradoException("Contrato no encontrado.");
        if (!contrato.EsOperable)
        {
            throw new ReglaNegocioException("Solo se puede cobrar un contrato vigente.");
        }

        var turno = await TurnoDeAsync(request.TurnoId, "PS", contrato.ClienteId, ct);
        var pago = new Pago
        {
            ContratoId = contrato.Id,
            ClienteId = contrato.ClienteId,
            Monto = contrato.Servicio.Precio,
            FechaPago = reloj.UtcAhora,
            FormaPagoId = contrato.FormaPagoId,
            TurnoId = turno.Id,
            CajeroId = actual.Id,
            Referencia = string.IsNullOrWhiteSpace(request.Referencia) ? null : request.Referencia.Trim()
        };
        await pagos.AgregarAsync(pago, ct);
        turno.MarcarProceso();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de pago {PagoId} del contrato {Numero} por {Actor}.", pago.Id, contrato.Numero, actual.NombreUsuario);
        var lista = await pagos.ListarPorClienteAsync(contrato.ClienteId, ct);
        return mapeador.Pago(lista.First(p => p.Id == pago.Id));
    }

    private async Task<Turno> TurnoDeAsync(int turnoId, string codigoTipo, int clienteId, CancellationToken ct)
    {
        var turno = await turnos.ObtenerPorIdAsync(turnoId, ct)
            ?? throw new NoEncontradoException("Turno no encontrado.");
        if (turno.CajeroId != actual.Id || turno.Estado != EstadoTurno.EnAtencion)
        {
            throw new ReglaNegocioException("El turno debe estar en atención y asignado a usted.");
        }

        if (!string.Equals(turno.TipoAtencion.Codigo, codigoTipo, StringComparison.OrdinalIgnoreCase))
        {
            throw new ReglaNegocioException($"Este turno es {turno.TipoAtencion.Codigo} ({turno.TipoAtencion.Nombre}) y no corresponde a la operación solicitada.");
        }

        var caja = await cajas.ObtenerPorIdAsync(turno.CajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        if (caja.UsuarioActivoId != actual.Id)
        {
            throw new ReglaNegocioException("La caja del turno no está abierta por usted.");
        }

        turno.VincularCliente(clienteId);
        return turno;
    }

    private async Task<Cliente> ClienteActivoAsync(int id, CancellationToken ct)
    {
        var cliente = await clientes.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Cliente no encontrado.");
        cliente.AsegurarActivo();
        return cliente;
    }

    private async Task<Servicio> ServicioActivoAsync(int id, CancellationToken ct)
    {
        var servicio = await catalogos.ObtenerServicioAsync(id, ct)
            ?? throw new NoEncontradoException("Servicio no encontrado.");
        if (!servicio.Activo || servicio.Eliminado)
        {
            throw new ReglaNegocioException("El servicio no está disponible.");
        }

        return servicio;
    }

    private async Task<FormaPago> FormaActivaAsync(int id, CancellationToken ct)
    {
        var forma = await catalogos.ObtenerFormaPagoAsync(id, ct)
            ?? throw new NoEncontradoException("Forma de pago no encontrada.");
        if (!forma.Activo)
        {
            throw new ReglaNegocioException("La forma de pago no está disponible.");
        }

        return forma;
    }
}
