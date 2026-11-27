using Microsoft.EntityFrameworkCore;
using NetDesk.Domain.Entities;

namespace NetDesk.Infrastructure.Persistence;

public class NetDeskDbContext(DbContextOptions<NetDeskDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenRecuperacion> TokensRecuperacion => Set<TokenRecuperacion>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<AsignacionCaja> AsignacionesCaja => Set<AsignacionCaja>();
    public DbSet<TipoAtencion> TiposAtencion => Set<TipoAtencion>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<FormaPago> FormasPago => Set<FormaPago>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MenuOpcion> Menus => Set<MenuOpcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NetDeskDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
