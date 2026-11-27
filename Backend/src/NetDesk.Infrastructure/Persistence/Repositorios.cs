using System.Data;
using Microsoft.EntityFrameworkCore;
using NetDesk.Application.Abstracciones;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Infrastructure.Persistence;

public class UnidadDeTrabajo(NetDeskDbContext db) : IUnidadDeTrabajo
{
    public Task<int> GuardarAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task EjecutarEnTransaccionAsync(Func<CancellationToken, Task> accion, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            await accion(ct);
            return;
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await accion(ct);
        await transaccion.CommitAsync(ct);
    }

    public void LimpiarSeguimiento() => db.ChangeTracker.Clear();
}

public class UsuarioRepositorio(NetDeskDbContext db) : IUsuarioRepositorio
{
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken ct = default) =>
        db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<Usuario?> ObtenerPorCorreoIndiceAsync(string indice, CancellationToken ct = default) =>
        db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.CorreoIndice == indice, ct);

    public Task<bool> ExisteNombreAsync(string nombreUsuario, int? exceptoId, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario && (exceptoId == null || u.Id != exceptoId), ct);

    public Task<bool> ExisteIdentificacionAsync(string indice, int? exceptoId, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.IdentificacionIndice == indice && (exceptoId == null || u.Id != exceptoId), ct);

    public Task<bool> ExisteCorreoAsync(string indice, int? exceptoId, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.CorreoIndice == indice && (exceptoId == null || u.Id != exceptoId), ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking().Include(u => u.Rol).OrderBy(u => u.Apellidos).ThenBy(u => u.Nombres).ToListAsync(ct);

    public async Task<IReadOnlyList<Usuario>> ListarPendientesAsync(CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking().Include(u => u.Rol)
            .Where(u => u.Estado == EstadoUsuario.PendienteAprobacion)
            .OrderBy(u => u.FechaCreacion)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Usuario>> ListarPorRolAsync(string rol, CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking().Include(u => u.Rol)
            .Where(u => u.Rol.Nombre == rol)
            .OrderBy(u => u.Apellidos)
            .ToListAsync(ct);

    public Task AgregarAsync(Usuario usuario, CancellationToken ct = default) => db.Usuarios.AddAsync(usuario, ct).AsTask();

    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct = default) =>
        db.Usuarios.CountAsync(u => u.Rol.Nombre == "Administrador" && u.Estado == EstadoUsuario.Activo, ct);
}

public class TokenRecuperacionRepositorio(NetDeskDbContext db) : ITokenRecuperacionRepositorio
{
    public Task AgregarAsync(TokenRecuperacion token, CancellationToken ct = default) =>
        db.TokensRecuperacion.AddAsync(token, ct).AsTask();

    public Task<TokenRecuperacion?> ObtenerVigentePorHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.TokensRecuperacion.FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsadoEn == null && t.ExpiraEn > DateTime.UtcNow, ct);

    public async Task InvalidarPendientesAsync(int usuarioId, CancellationToken ct = default)
    {
        var pendientes = await db.TokensRecuperacion.Where(t => t.UsuarioId == usuarioId && t.UsadoEn == null).ToListAsync(ct);
        foreach (var token in pendientes)
        {
            token.MarcarUsado();
        }
    }
}

public class ClienteRepositorio(NetDeskDbContext db) : IClienteRepositorio
{
    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> ObtenerPorIndiceAsync(string indice, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.IdentificacionIndice == indice, ct);

    public Task<bool> ExisteIdentificacionAsync(string indice, int? exceptoId, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.IdentificacionIndice == indice && (exceptoId == null || c.Id != exceptoId), ct);

    public Task<bool> ExisteCorreoAsync(string indice, int? exceptoId, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.CorreoIndice == indice && (exceptoId == null || c.Id != exceptoId), ct);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? texto, string? indiceIdentificacion, CancellationToken ct = default)
    {
        var consulta = db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(indiceIdentificacion))
        {
            consulta = consulta.Where(c => c.IdentificacionIndice == indiceIdentificacion);
        }
        else if (!string.IsNullOrWhiteSpace(texto))
        {
            var criterio = texto.Trim().ToLower();
            consulta = consulta.Where(c =>
                c.Nombres.ToLower().Contains(criterio) ||
                c.Apellidos.ToLower().Contains(criterio) ||
                c.Telefono.Contains(texto.Trim()));
        }

        return await consulta.OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres).ToListAsync(ct);
    }

    public Task AgregarAsync(Cliente cliente, CancellationToken ct = default) => db.Clientes.AddAsync(cliente, ct).AsTask();
}

public class CajaRepositorio(NetDeskDbContext db) : ICajaRepositorio
{
    private IQueryable<Caja> Consulta() =>
        db.Cajas
            .Include(c => c.UsuarioActivo)
            .Include(c => c.Asignaciones.Where(a => a.Activa))
            .ThenInclude(a => a.Usuario);

    public async Task<IReadOnlyList<Caja>> ListarAsync(CancellationToken ct = default) =>
        await Consulta().AsNoTracking().OrderBy(c => c.Codigo).ToListAsync(ct);

    public Task<Caja?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        Consulta().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Caja?> ObtenerAbiertaPorUsuarioAsync(int usuarioId, CancellationToken ct = default) =>
        db.Cajas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioActivoId == usuarioId, ct);

    public Task<bool> CajeroYaAsignadoAsync(int cajeroId, CancellationToken ct = default) =>
        db.AsignacionesCaja.AnyAsync(a => a.Activa && a.UsuarioId == cajeroId, ct);

    public Task AgregarAsync(Caja caja, CancellationToken ct = default) => db.Cajas.AddAsync(caja, ct).AsTask();

    public async Task<bool> IntentarAbrirAsync(int cajaId, int usuarioId, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var afectados = await db.Cajas
            .Where(c => c.Id == cajaId && (c.UsuarioActivoId == null || c.UsuarioActivoId == usuarioId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.UsuarioActivoId, usuarioId)
                .SetProperty(c => c.FechaAperturaSesion, ahora)
                .SetProperty(c => c.FechaModificacion, ahora), ct);
        db.ChangeTracker.Clear();
        return afectados == 1;
    }

    public async Task<bool> IntentarCerrarAsync(int cajaId, int usuarioId, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var afectados = await db.Cajas
            .Where(c => c.Id == cajaId && c.UsuarioActivoId == usuarioId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.UsuarioActivoId, (int?)null)
                .SetProperty(c => c.FechaAperturaSesion, (DateTime?)null)
                .SetProperty(c => c.FechaModificacion, ahora), ct);
        db.ChangeTracker.Clear();
        return afectados == 1;
    }
}
