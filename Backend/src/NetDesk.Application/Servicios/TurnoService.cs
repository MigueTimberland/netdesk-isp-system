using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Application.Servicios;

public sealed class TurnoService(
    ITurnoRepositorio turnos,
    ICajaRepositorio cajas,
    IUsuarioRepositorio usuarios,
    ICatalogoRepositorio catalogos,
    IClienteRepositorio clientes,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    IReloj reloj,
    Mapeador mapeador,
    IValidator<CrearTurnosRequest> crearValidator,
    ILogger<TurnoService> logger)
{
    public async Task<IReadOnlyList<TurnoDto>> ListarHoyAsync(int? cajaId, CancellationToken ct)
    {
        var (inicio, fin) = reloj.RangoDia(reloj.HoyOperacion);
        int? gestorId = actual.Rol == "Gestor" ? actual.Id : null;
        int? cajeroId = actual.Rol == "Cajero" ? actual.Id : null;
        var lista = await turnos.ListarAsync(inicio, fin, cajaId, cajeroId, gestorId, ct);
        return lista.Select(mapeador.Turno).ToList();
    }

    public async Task<IReadOnlyList<TurnoDto>> ListarMiosAsync(CancellationToken ct)
    {
        var (inicio, fin) = reloj.RangoDia(reloj.HoyOperacion);
        var lista = await turnos.ListarAsync(inicio, fin, null, actual.Id, null, ct);
        return lista.Select(mapeador.Turno).ToList();
    }

    public async Task<IReadOnlyList<TurnoDto>> CrearAsync(CrearTurnosRequest request, CancellationToken ct)
    {
        await crearValidator.ValidarAsync(request, ct);
        var caja = await cajas.ObtenerPorIdAsync(request.CajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        if (caja.Estado != EstadoCaja.Activa)
        {
            throw new ReglaNegocioException("La caja no está activa.");
        }

        var asignado = caja.Asignaciones.Any(a => a.Activa && !a.Eliminado && a.UsuarioId == request.CajeroId);
        if (!asignado)
        {
            throw new ReglaNegocioException("El cajero no está asignado a la caja seleccionada.");
        }

        var cajero = await usuarios.ObtenerPorIdAsync(request.CajeroId, ct)
            ?? throw new NoEncontradoException("Cajero no encontrado.");
        if (cajero.Rol.Nombre != "Cajero" || cajero.Estado != EstadoUsuario.Activo)
        {
            throw new ReglaNegocioException("El cajero no está activo.");
        }

        var tipo = await catalogos.ObtenerTipoAsync(request.TipoAtencionId, ct)
            ?? throw new NoEncontradoException("El tipo de atención no existe.");

        var creados = new List<Turno>();
        await unidad.EjecutarEnTransaccionAsync(async token =>
        {
            for (var i = 0; i < request.Cantidad; i++)
            {
                var codigo = await turnos.GenerarCodigoAsync(tipo.Codigo, token);
                var turno = new Turno
                {
                    Descripcion = codigo,
                    TipoAtencionId = tipo.Id,
                    CajaId = caja.Id,
                    CajeroId = cajero.Id,
                    GestorAsignadorId = actual.Id,
                    Estado = EstadoTurno.Asignado,
                    FechaAsignacion = reloj.UtcAhora
                };
                await turnos.AgregarAsync(turno, token);
                await unidad.GuardarAsync(token);
                creados.Add(turno);
            }
        }, ct);

        logger.LogInformation("Creación de {Cantidad} turnos {Tipo} en caja {Caja} por {Actor}.", request.Cantidad, tipo.Codigo, caja.Codigo, actual.NombreUsuario);
        var (inicio, fin) = reloj.RangoDia(reloj.HoyOperacion);
        var lista = await turnos.ListarAsync(inicio, fin, caja.Id, null, actual.Id, ct);
        var ids = creados.Select(t => t.Descripcion).ToHashSet();
        return lista.Where(t => ids.Contains(t.Descripcion)).Select(mapeador.Turno).ToList();
    }

    public async Task<TurnoDto> IniciarAsync(int id, IniciarTurnoRequest request, CancellationToken ct)
    {
        var turno = await turnos.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Turno no encontrado.");
        var caja = await cajas.ObtenerPorIdAsync(turno.CajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        if (caja.UsuarioActivoId != actual.Id)
        {
            throw new ReglaNegocioException("Abra la caja antes de atender turnos. Otro cajero no puede operarla al mismo tiempo.");
        }

        if (await turnos.TieneEnAtencionAsync(actual.Id, ct))
        {
            throw new ReglaNegocioException("Ya tiene un turno en atención. Finalícelo antes de tomar otro.");
        }

        if (request.ClienteId.HasValue)
        {
            _ = await clientes.ObtenerPorIdAsync(request.ClienteId.Value, ct)
                ?? throw new NoEncontradoException("Cliente no encontrado.");
        }

        turno.Iniciar(actual.Id, request.ClienteId);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización: turno {Turno} iniciado por {Actor}.", turno.Descripcion, actual.NombreUsuario);
        return mapeador.Turno(await turnos.ObtenerPorIdAsync(id, ct) ?? turno);
    }

    public async Task<TurnoDto> FinalizarAsync(int id, CancellationToken ct)
    {
        var turno = await turnos.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Turno no encontrado.");
        if (turno.CajeroId != actual.Id)
        {
            throw new ReglaNegocioException("Este turno está asignado a otro cajero.");
        }

        turno.Finalizar();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización: turno {Turno} atendido por {Actor}.", turno.Descripcion, actual.NombreUsuario);
        return mapeador.Turno(turno);
    }

    public async Task CancelarAsync(int id, CancellationToken ct)
    {
        var turno = await turnos.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Turno no encontrado.");
        turno.Cancelar();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Eliminación operativa del turno {Turno} por {Actor}.", turno.Descripcion, actual.NombreUsuario);
    }
}
