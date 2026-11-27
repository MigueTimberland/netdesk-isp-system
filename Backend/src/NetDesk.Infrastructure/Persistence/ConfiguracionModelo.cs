using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetDesk.Domain.Entities;

namespace NetDesk.Infrastructure.Persistence;

internal static class Filtro
{
    public static void Borrado<T>(EntityTypeBuilder<T> builder) where T : Domain.Common.EntidadAuditable
    {
        builder.HasQueryFilter(e => !e.Eliminado);
        builder.Property(e => e.FechaCreacion).HasColumnType("datetime2");
        builder.Property(e => e.FechaModificacion).HasColumnType("datetime2");
        builder.Property(e => e.FechaEliminacion).HasColumnType("datetime2");
    }
}

public class RolConfig : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");
        builder.Property(x => x.Nombre).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        Filtro.Borrado(builder);
        builder.Property(x => x.NombreUsuario).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Nombres).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Apellidos).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IdentificacionCifrada).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IdentificacionIndice).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorreoCifrado).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CorreoIndice).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Telefono).HasMaxLength(15).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.HasIndex(x => x.NombreUsuario).IsUnique().HasFilter("[Eliminado] = 0");
        builder.HasIndex(x => x.IdentificacionIndice).IsUnique().HasFilter("[Eliminado] = 0");
        builder.HasIndex(x => x.CorreoIndice).IsUnique().HasFilter("[Eliminado] = 0");
        builder.HasOne(x => x.Rol).WithMany(x => x.Usuarios).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AprobadoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TokenRecuperacionConfig : IEntityTypeConfiguration<TokenRecuperacion>
{
    public void Configure(EntityTypeBuilder<TokenRecuperacion> builder)
    {
        builder.ToTable("TokensRecuperacion");
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExpiraEn).HasColumnType("datetime2");
        builder.Property(x => x.UsadoEn).HasColumnType("datetime2");
        builder.Property(x => x.FechaCreacion).HasColumnType("datetime2");
        builder.HasIndex(x => x.TokenHash);
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CajaConfig : IEntityTypeConfiguration<Caja>
{
    public void Configure(EntityTypeBuilder<Caja> builder)
    {
        builder.ToTable("Cajas");
        Filtro.Borrado(builder);
        builder.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.Property(x => x.FechaAperturaSesion).HasColumnType("datetime2");
        builder.HasIndex(x => x.Codigo).IsUnique().HasFilter("[Eliminado] = 0");
        builder.HasOne(x => x.UsuarioActivo).WithMany().HasForeignKey(x => x.UsuarioActivoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AsignacionCajaConfig : IEntityTypeConfiguration<AsignacionCaja>
{
    public void Configure(EntityTypeBuilder<AsignacionCaja> builder)
    {
        builder.ToTable("AsignacionesCaja");
        Filtro.Borrado(builder);
        builder.HasOne(x => x.Caja).WithMany(x => x.Asignaciones).HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AsignadoPor).WithMany().HasForeignKey(x => x.AsignadoPorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TipoAtencionConfig : IEntityTypeConfiguration<TipoAtencion>
{
    public void Configure(EntityTypeBuilder<TipoAtencion> builder)
    {
        builder.ToTable("TiposAtencion");
        builder.Property(x => x.Codigo).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.HasIndex(x => x.Codigo).IsUnique();
    }
}

public class TurnoConfig : IEntityTypeConfiguration<Turno>
{
    public void Configure(EntityTypeBuilder<Turno> builder)
    {
        builder.ToTable("Turnos");
        Filtro.Borrado(builder);
        builder.Property(x => x.Descripcion).HasMaxLength(6).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.Property(x => x.FechaAsignacion).HasColumnType("datetime2");
        builder.Property(x => x.FechaInicioAtencion).HasColumnType("datetime2");
        builder.Property(x => x.FechaFinAtencion).HasColumnType("datetime2");
        builder.HasIndex(x => x.Descripcion).IsUnique();
        builder.HasOne(x => x.TipoAtencion).WithMany(x => x.Turnos).HasForeignKey(x => x.TipoAtencionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Caja).WithMany(x => x.Turnos).HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cajero).WithMany().HasForeignKey(x => x.CajeroId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GestorAsignador).WithMany().HasForeignKey(x => x.GestorAsignadorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        Filtro.Borrado(builder);
        builder.Property(x => x.Nombres).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Apellidos).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IdentificacionCifrada).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IdentificacionIndice).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorreoCifrado).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CorreoIndice).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Telefono).HasMaxLength(15).IsRequired();
        builder.Property(x => x.Direccion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReferenciaDireccion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.HasIndex(x => x.IdentificacionIndice).IsUnique().HasFilter("[Eliminado] = 0");
        builder.HasIndex(x => x.CorreoIndice).IsUnique().HasFilter("[Eliminado] = 0");
    }
}

public class ServicioConfig : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.ToTable("Servicios");
        Filtro.Borrado(builder);
        builder.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Equipos).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Precio).HasColumnType("decimal(12,2)");
    }
}

public class FormaPagoConfig : IEntityTypeConfiguration<FormaPago>
{
    public void Configure(EntityTypeBuilder<FormaPago> builder)
    {
        builder.ToTable("FormasPago");
        builder.Property(x => x.Nombre).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class ContratoConfig : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("Contratos");
        Filtro.Borrado(builder);
        builder.Property(x => x.Numero).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.Property(x => x.FechaInicio).HasColumnType("datetime2");
        builder.Property(x => x.FechaFin).HasColumnType("datetime2");
        builder.Property(x => x.Observacion).HasMaxLength(300);
        builder.HasIndex(x => x.Numero).IsUnique();
        builder.HasOne(x => x.Cliente).WithMany(x => x.Contratos).HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Servicio).WithMany(x => x.Contratos).HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FormaPago).WithMany(x => x.Contratos).HasForeignKey(x => x.FormaPagoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ContratoOrigen).WithMany().HasForeignKey(x => x.ContratoOrigenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Turno).WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cajero).WithMany().HasForeignKey(x => x.CajeroId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PagoConfig : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos");
        Filtro.Borrado(builder);
        builder.Property(x => x.Monto).HasColumnType("decimal(12,2)");
        builder.Property(x => x.FechaPago).HasColumnType("datetime2");
        builder.Property(x => x.Referencia).HasMaxLength(80);
        builder.HasOne(x => x.Contrato).WithMany(x => x.Pagos).HasForeignKey(x => x.ContratoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FormaPago).WithMany().HasForeignKey(x => x.FormaPagoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Turno).WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cajero).WithMany().HasForeignKey(x => x.CajeroId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MenuOpcionConfig : IEntityTypeConfiguration<MenuOpcion>
{
    public void Configure(EntityTypeBuilder<MenuOpcion> builder)
    {
        builder.ToTable("Menus");
        builder.Property(x => x.Titulo).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Ruta).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Icono).HasMaxLength(40).IsRequired();
        builder.HasOne(x => x.Rol).WithMany(x => x.Menus).HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Cascade);
    }
}
