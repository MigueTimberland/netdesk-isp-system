using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetDesk.Application.Abstracciones;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Infrastructure.Persistence;

public class TurnoRepositorio(NetDeskDbContext db) : ITurnoRepositorio
{
    private IQueryable<Turno> Consulta() =>
        db.Turnos
            .Include(t => t.TipoAtencion)
            .Include(t => t.Caja)
            .Include(t => t.Cajero)
            .Include(t => t.GestorAsignador)
            .Include(t => t.Cliente);

    public Task<Turno?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Consulta().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Turno>> ListarAsync(DateTime inicioUtc, DateTime finUtc, int? cajaId, int? cajeroId, int? gestorId, CancellationToken ct = default)
    {
        var consulta = Consulta().AsNoTracking().Where(t => t.FechaAsignacion >= inicioUtc && t.FechaAsignacion < finUtc);
        if (cajaId.HasValue) consulta = consulta.Where(t => t.CajaId == cajaId);
        if (cajeroId.HasValue) consulta = consulta.Where(t => t.CajeroId == cajeroId);
        if (gestorId.HasValue) consulta = consulta.Where(t => t.GestorAsignadorId == gestorId);
        return await consulta.OrderBy(t => t.Descripcion).ToListAsync(ct);
    }

    public Task<bool> TieneEnAtencionAsync(int cajeroId, CancellationToken ct = default) =>
        db.Turnos.AnyAsync(t => t.CajeroId == cajeroId && t.Estado == EstadoTurno.EnAtencion, ct);

    public Task AgregarAsync(Turno turno, CancellationToken ct = default) => db.Turnos.AddAsync(turno, ct).AsTask();

    public async Task<string> GenerarCodigoAsync(string prefijo, CancellationToken ct = default)
    {
        var conexion = db.Database.GetDbConnection();
        var abiertaAqui = conexion.State != ConnectionState.Open;
        if (abiertaAqui)
        {
            await conexion.OpenAsync(ct);
        }

        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "dbo.sp_GenerarCodigoTurno";
            comando.CommandType = CommandType.StoredProcedure;
            comando.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            var prefijoParam = comando.CreateParameter();
            prefijoParam.ParameterName = "@Prefijo";
            prefijoParam.Value = prefijo;
            comando.Parameters.Add(prefijoParam);
            var salida = comando.CreateParameter();
            salida.ParameterName = "@Codigo";
            salida.DbType = DbType.String;
            salida.Size = 6;
            salida.Direction = ParameterDirection.Output;
            comando.Parameters.Add(salida);
            await comando.ExecuteNonQueryAsync(ct);
            return Convert.ToString(salida.Value) ?? throw new InvalidOperationException("El procedimiento no devolvió el código del turno.");
        }
        finally
        {
            if (abiertaAqui)
            {
                await conexion.CloseAsync();
            }
        }
    }
}

public class ContratoRepositorio(NetDeskDbContext db) : IContratoRepositorio
{
    private IQueryable<Contrato> Consulta() =>
        db.Contratos.Include(c => c.Cliente).Include(c => c.Servicio).Include(c => c.FormaPago);

    public Task<Contrato?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Consulta().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Contrato?> ObtenerOperablePorClienteAsync(int clienteId, CancellationToken ct = default) =>
        Consulta().FirstOrDefaultAsync(c => c.ClienteId == clienteId && (c.Estado == EstadoContrato.Vigente || c.Estado == EstadoContrato.Renovacion), ct);

    public async Task<IReadOnlyList<Contrato>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default) =>
        await Consulta().AsNoTracking().Where(c => c.ClienteId == clienteId).OrderByDescending(c => c.FechaInicio).ToListAsync(ct);

    public async Task<string> SiguienteNumeroAsync(CancellationToken ct = default)
    {
        var total = await db.Contratos.IgnoreQueryFilters().CountAsync(ct);
        return $"CT-{(total + 1):000000}";
    }

    public Task AgregarAsync(Contrato contrato, CancellationToken ct = default) => db.Contratos.AddAsync(contrato, ct).AsTask();
}

public class PagoRepositorio(NetDeskDbContext db) : IPagoRepositorio
{
    public async Task<IReadOnlyList<Pago>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default) =>
        await db.Pagos.AsNoTracking()
            .Include(p => p.Contrato)
            .Include(p => p.FormaPago)
            .Include(p => p.Cajero)
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync(ct);

    public Task AgregarAsync(Pago pago, CancellationToken ct = default) => db.Pagos.AddAsync(pago, ct).AsTask();
}

public class CatalogoRepositorio(NetDeskDbContext db) : ICatalogoRepositorio
{
    public Task<Rol?> ObtenerRolAsync(int id, CancellationToken ct = default) => db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Rol?> ObtenerRolPorNombreAsync(string nombre, CancellationToken ct = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre, ct);

    public async Task<IReadOnlyList<Rol>> ListarRolesOperativosAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking().Where(r => r.Nombre == "Gestor" || r.Nombre == "Cajero").OrderBy(r => r.Nombre).ToListAsync(ct);

    public Task<TipoAtencion?> ObtenerTipoAsync(int id, CancellationToken ct = default) =>
        db.TiposAtencion.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TipoAtencion>> ListarTiposAsync(CancellationToken ct = default) =>
        await db.TiposAtencion.AsNoTracking().OrderBy(t => t.Codigo).ToListAsync(ct);

    public Task<FormaPago?> ObtenerFormaPagoAsync(int id, CancellationToken ct = default) =>
        db.FormasPago.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<FormaPago>> ListarFormasPagoAsync(CancellationToken ct = default) =>
        await db.FormasPago.AsNoTracking().Where(f => f.Activo).OrderBy(f => f.Nombre).ToListAsync(ct);

    public Task<Servicio?> ObtenerServicioAsync(int id, CancellationToken ct = default) =>
        db.Servicios.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Servicio>> ListarServiciosAsync(bool soloActivos, CancellationToken ct = default)
    {
        var consulta = db.Servicios.AsNoTracking().AsQueryable();
        if (soloActivos) consulta = consulta.Where(s => s.Activo);
        return await consulta.OrderBy(s => s.VelocidadMbps).ToListAsync(ct);
    }

    public Task AgregarServicioAsync(Servicio servicio, CancellationToken ct = default) => db.Servicios.AddAsync(servicio, ct).AsTask();

    public async Task<IReadOnlyList<MenuOpcion>> ListarMenusPorRolAsync(int rolId, CancellationToken ct = default) =>
        await db.Menus.AsNoTracking().Where(m => m.RolId == rolId && m.Activo).OrderBy(m => m.Orden).ToListAsync(ct);
}

public class DashboardRepositorio(NetDeskDbContext db) : IDashboardRepositorio
{
    public async Task<int> TotalTurnosAsync(int usuarioId, string rol, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
    {
        var conexion = db.Database.GetDbConnection();
        var abiertaAqui = conexion.State != ConnectionState.Open;
        if (abiertaAqui) await conexion.OpenAsync(ct);
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT dbo.fn_TotalTurnosRango(@UsuarioId, @Rol, @Inicio, @Fin)";
            Agregar(comando, "@UsuarioId", usuarioId);
            Agregar(comando, "@Rol", rol);
            Agregar(comando, "@Inicio", inicioUtc);
            Agregar(comando, "@Fin", finUtc);
            var valor = await comando.ExecuteScalarAsync(ct);
            return Convert.ToInt32(valor);
        }
        finally
        {
            if (abiertaAqui) await conexion.CloseAsync();
        }
    }

    public async Task<DashboardCrudo> IndicadoresAsync(DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
    {
        var conexion = db.Database.GetDbConnection();
        var abiertaAqui = conexion.State != ConnectionState.Open;
        if (abiertaAqui) await conexion.OpenAsync(ct);
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "dbo.sp_DashboardIndicadores";
            comando.CommandType = CommandType.StoredProcedure;
            Agregar(comando, "@FechaInicio", inicioUtc);
            Agregar(comando, "@FechaFin", finUtc);
            await using var lector = await comando.ExecuteReaderAsync(ct);
            var resultado = new DashboardCrudo();
            if (await lector.ReadAsync(ct))
            {
                resultado.SesionActiva = lector.IsDBNull(0) ? 0 : lector.GetInt32(0);
                resultado.SesionInactiva = lector.IsDBNull(1) ? 0 : lector.GetInt32(1);
                resultado.Bloqueados = lector.IsDBNull(2) ? 0 : lector.GetInt32(2);
                resultado.Pendientes = lector.IsDBNull(3) ? 0 : lector.GetInt32(3);
                resultado.Activos = lector.IsDBNull(4) ? 0 : lector.GetInt32(4);
                resultado.Inactivos = lector.IsDBNull(5) ? 0 : lector.GetInt32(5);
            }

            await lector.NextResultAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Cajas.Add(new FilaCaja
                {
                    Id = lector.GetInt32(0),
                    Codigo = lector.GetString(1),
                    Nombre = lector.GetString(2),
                    TurnosAsignados = lector.GetInt32(3),
                    TurnosAtendidos = lector.GetInt32(4)
                });
            }

            await lector.NextResultAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Cajeros.Add(LeerPersona(lector));
            }

            await lector.NextResultAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                resultado.Gestores.Add(LeerPersona(lector));
            }

            return resultado;
        }
        finally
        {
            if (abiertaAqui) await conexion.CloseAsync();
        }
    }

    private static FilaPersona LeerPersona(System.Data.Common.DbDataReader lector) => new()
    {
        Id = lector.GetInt32(0),
        Nombre = lector.GetString(1),
        Usuario = lector.GetString(2),
        Total = lector.GetInt32(3)
    };

    private static void Agregar(System.Data.Common.DbCommand comando, string nombre, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
