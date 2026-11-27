using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;

namespace NetDesk.Application.Servicios;

public sealed class CatalogoService(
    ICatalogoRepositorio catalogos,
    IUsuarioRepositorio usuarios,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    Mapeador mapeador,
    IValidator<GuardarServicioRequest> validator,
    ILogger<CatalogoService> logger)
{
    public async Task<IReadOnlyList<CatalogoDto>> RolesOperativosAsync(CancellationToken ct)
    {
        var roles = await catalogos.ListarRolesOperativosAsync(ct);
        return roles.Select(r => new CatalogoDto(r.Id, r.Nombre, null)).ToList();
    }

    public async Task<IReadOnlyList<CatalogoDto>> TiposAtencionAsync(CancellationToken ct)
    {
        var tipos = await catalogos.ListarTiposAsync(ct);
        return tipos.Select(t => new CatalogoDto(t.Id, t.Nombre, t.Codigo)).ToList();
    }

    public async Task<IReadOnlyList<CatalogoDto>> FormasPagoAsync(CancellationToken ct)
    {
        var formas = await catalogos.ListarFormasPagoAsync(ct);
        return formas.Select(f => new CatalogoDto(f.Id, f.Nombre, null)).ToList();
    }

    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(bool soloActivos, CancellationToken ct)
    {
        var servicios = await catalogos.ListarServiciosAsync(soloActivos, ct);
        return servicios.Select(mapeador.Servicio).ToList();
    }

    public async Task<ServicioDto> CrearServicioAsync(GuardarServicioRequest request, CancellationToken ct)
    {
        await validator.ValidarAsync(request, ct);
        var servicio = new Servicio();
        Aplicar(servicio, request);
        await catalogos.AgregarServicioAsync(servicio, ct);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de servicio {Servicio} por {Actor}.", servicio.Nombre, actual.NombreUsuario);
        return mapeador.Servicio(servicio);
    }

    public async Task<ServicioDto> ActualizarServicioAsync(int id, GuardarServicioRequest request, CancellationToken ct)
    {
        await validator.ValidarAsync(request, ct);
        var servicio = await catalogos.ObtenerServicioAsync(id, ct)
            ?? throw new NoEncontradoException("Servicio no encontrado.");
        Aplicar(servicio, request);
        servicio.Tocar();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de servicio {Servicio} por {Actor}.", servicio.Nombre, actual.NombreUsuario);
        return mapeador.Servicio(servicio);
    }

    public async Task EliminarServicioAsync(int id, CancellationToken ct)
    {
        var servicio = await catalogos.ObtenerServicioAsync(id, ct)
            ?? throw new NoEncontradoException("Servicio no encontrado.");
        servicio.Activo = false;
        servicio.MarcarEliminado();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Eliminación lógica del servicio {Servicio} por {Actor}.", servicio.Nombre, actual.NombreUsuario);
    }

    public async Task<IReadOnlyList<MenuDto>> MisMenusAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(actual.Id, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");
        var menus = await catalogos.ListarMenusPorRolAsync(usuario.RolId, ct);
        return menus.Select(m => new MenuDto(m.Id, m.Titulo, m.Ruta, m.Icono, m.Orden)).ToList();
    }

    private static void Aplicar(Servicio servicio, GuardarServicioRequest request)
    {
        servicio.Nombre = request.Nombre.Trim();
        servicio.Descripcion = request.Descripcion.Trim();
        servicio.Equipos = request.Equipos.Trim();
        servicio.VelocidadMbps = request.VelocidadMbps;
        servicio.Precio = request.Precio;
        servicio.Activo = request.Activo;
    }
}

public sealed class DashboardService(
    IDashboardRepositorio dashboard,
    IUsuarioRepositorio usuarios,
    IUsuarioActual actual,
    IReloj reloj,
    Mapeador mapeador)
{
    public async Task<BienvenidaDto> BienvenidaAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(actual.Id, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");
        var (inicio, fin) = reloj.RangoDia(reloj.HoyOperacion);
        var total = await dashboard.TotalTurnosAsync(usuario.Id, usuario.Rol.Nombre, inicio, fin, ct);
        var descripcion = usuario.Rol.Nombre switch
        {
            "Administrador" => "Turnos atendidos hoy en todas las cajas",
            "Gestor" => "Turnos que usted asignó hoy a los cajeros",
            _ => "Turnos que usted atendió hoy"
        };
        var pendientes = usuario.Rol.Nombre == "Gestor"
            ? (await usuarios.ListarPendientesAsync(ct)).Select(mapeador.Usuario).ToList()
            : [];
        return new BienvenidaDto(mapeador.Sesion(usuario), total, descripcion, pendientes);
    }

    public async Task<DashboardDto> IndicadoresAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var inicioFecha = desde ?? reloj.HoyOperacion;
        var finFecha = hasta ?? inicioFecha;
        if (finFecha < inicioFecha)
        {
            throw new ReglaNegocioException("La fecha fin no puede ser menor que la fecha inicio.");
        }

        var (inicio, _) = reloj.RangoDia(inicioFecha);
        var (_, fin) = reloj.RangoDia(finFecha);
        var crudo = await dashboard.IndicadoresAsync(inicio, fin, ct);
        return new DashboardDto(
            inicioFecha,
            finFecha,
            new IndicadoresUsuariosDto(crudo.SesionActiva, crudo.SesionInactiva, crudo.Bloqueados, crudo.Pendientes, crudo.Activos, crudo.Inactivos),
            crudo.Cajas.Select(c => new IndicadorCajaDto(c.Id, c.Codigo, c.Nombre, c.TurnosAsignados, c.TurnosAtendidos)).ToList(),
            crudo.Cajeros.Select(c => new IndicadorPersonaDto(c.Id, c.Nombre, c.Usuario, c.Total)).ToList(),
            crudo.Gestores.Select(c => new IndicadorPersonaDto(c.Id, c.Nombre, c.Usuario, c.Total)).ToList());
    }
}
