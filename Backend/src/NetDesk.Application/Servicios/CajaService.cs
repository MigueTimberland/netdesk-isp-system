using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Application.Servicios;

public sealed class CajaService(
    ICajaRepositorio cajas,
    IUsuarioRepositorio usuarios,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    Mapeador mapeador,
    IValidator<CrearCajaRequest> crearValidator,
    IValidator<AsignarCajeroRequest> asignarValidator,
    ILogger<CajaService> logger)
{
    public async Task<IReadOnlyList<CajaDto>> ListarAsync(CancellationToken ct)
    {
        var lista = await cajas.ListarAsync(ct);
        return lista.Select(mapeador.Caja).ToList();
    }

    public async Task<IReadOnlyList<CajaDto>> ListarMiasAsync(CancellationToken ct)
    {
        var lista = await cajas.ListarAsync(ct);
        return lista
            .Where(c => c.Asignaciones.Any(a => a.Activa && !a.Eliminado && a.UsuarioId == actual.Id))
            .Select(mapeador.Caja)
            .ToList();
    }

    public async Task<CajaDto> CrearAsync(CrearCajaRequest request, CancellationToken ct)
    {
        await crearValidator.ValidarAsync(request, ct);
        var codigo = request.Codigo.Trim().ToUpperInvariant();
        var existentes = await cajas.ListarAsync(ct);
        if (existentes.Any(c => c.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ReglaNegocioException("Ya existe una caja con ese código.");
        }

        var caja = new Caja
        {
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            Estado = EstadoCaja.Activa
        };
        await cajas.AgregarAsync(caja, ct);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de caja {Codigo} por {Actor}.", caja.Codigo, actual.NombreUsuario);
        return mapeador.Caja(caja);
    }

    public async Task<CajaDto> AsignarAsync(int cajaId, AsignarCajeroRequest request, CancellationToken ct)
    {
        await asignarValidator.ValidarAsync(request, ct);
        var caja = await cajas.ObtenerPorIdAsync(cajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        var cajero = await usuarios.ObtenerPorIdAsync(request.CajeroId, ct)
            ?? throw new NoEncontradoException("Cajero no encontrado.");
        if (cajero.Rol.Nombre != "Cajero" || cajero.Estado != EstadoUsuario.Activo)
        {
            throw new ReglaNegocioException("Solo se puede asignar un cajero activo.");
        }

        if (await cajas.CajeroYaAsignadoAsync(cajero.Id, ct))
        {
            throw new ReglaNegocioException("El cajero ya está asignado a una caja. Un cajero opera una sola caja.");
        }

        caja.AsignarCajero(cajero.Id, actual.Id);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Asignación del cajero {Cajero} a la caja {Codigo} por {Actor}.", cajero.NombreUsuario, caja.Codigo, actual.NombreUsuario);
        return mapeador.Caja(await cajas.ObtenerPorIdAsync(cajaId, ct) ?? caja);
    }

    public async Task<CajaDto> QuitarAsignacionAsync(int cajaId, int asignacionId, CancellationToken ct)
    {
        var caja = await cajas.ObtenerPorIdAsync(cajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        caja.QuitarCajero(asignacionId);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Eliminación lógica de asignación {AsignacionId} en caja {Codigo} por {Actor}.", asignacionId, caja.Codigo, actual.NombreUsuario);
        return mapeador.Caja(await cajas.ObtenerPorIdAsync(cajaId, ct) ?? caja);
    }

    public async Task<CajaDto> AbrirAsync(int cajaId, CancellationToken ct)
    {
        var caja = await cajas.ObtenerPorIdAsync(cajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        var asignado = caja.Asignaciones.Any(a => a.Activa && !a.Eliminado && a.UsuarioId == actual.Id);
        if (!asignado)
        {
            throw new ReglaNegocioException("El cajero no está asignado a esta caja.");
        }

        var otra = await cajas.ObtenerAbiertaPorUsuarioAsync(actual.Id, ct);
        if (otra is not null && otra.Id != caja.Id)
        {
            throw new ReglaNegocioException($"Ya tiene abierta la caja {otra.Codigo}. Ciérrela antes de abrir otra.");
        }

        if (caja.UsuarioActivoId.HasValue && caja.UsuarioActivoId != actual.Id)
        {
            throw new ReglaNegocioException("Otro cajero ya está operando esta caja. Los dos asignados no pueden atender al mismo tiempo.");
        }

        var abierta = await cajas.IntentarAbrirAsync(caja.Id, actual.Id, ct);
        if (!abierta)
        {
            throw new ReglaNegocioException("No se pudo abrir la caja. Otro cajero la tomó en este momento.");
        }

        logger.LogInformation("Apertura de caja {Codigo} por {Actor}.", caja.Codigo, actual.NombreUsuario);
        return mapeador.Caja(await cajas.ObtenerPorIdAsync(cajaId, ct) ?? caja);
    }

    public async Task<CajaDto> CerrarAsync(int cajaId, CancellationToken ct)
    {
        var caja = await cajas.ObtenerPorIdAsync(cajaId, ct)
            ?? throw new NoEncontradoException("Caja no encontrada.");
        if (caja.UsuarioActivoId != actual.Id)
        {
            throw new ReglaNegocioException("No tiene una sesión abierta en esta caja.");
        }

        var cerrada = await cajas.IntentarCerrarAsync(caja.Id, actual.Id, ct);
        if (!cerrada)
        {
            throw new ReglaNegocioException("No se pudo cerrar la caja.");
        }

        logger.LogInformation("Cierre de caja {Codigo} por {Actor}.", caja.Codigo, actual.NombreUsuario);
        return mapeador.Caja(await cajas.ObtenerPorIdAsync(cajaId, ct) ?? caja);
    }
}
