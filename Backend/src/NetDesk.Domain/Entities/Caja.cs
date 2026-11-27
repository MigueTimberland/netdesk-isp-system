using NetDesk.Domain.Common;
using NetDesk.Domain.Enums;

namespace NetDesk.Domain.Entities;

public class Caja : EntidadAuditable
{
    public const int MaximoCajeros = 2;

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public EstadoCaja Estado { get; set; } = EstadoCaja.Activa;
    public int? UsuarioActivoId { get; set; }
    public Usuario? UsuarioActivo { get; set; }
    public DateTime? FechaAperturaSesion { get; set; }
    public ICollection<AsignacionCaja> Asignaciones { get; set; } = new List<AsignacionCaja>();
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public int CajerosActivos => Asignaciones.Count(a => a.Activa && !a.Eliminado);

    public void AsignarCajero(int cajeroId, int gestorId)
    {
        if (Estado != EstadoCaja.Activa || Eliminado)
        {
            throw new ReglaNegocioException("La caja no está activa.");
        }

        if (Asignaciones.Any(a => a.Activa && !a.Eliminado && a.UsuarioId == cajeroId))
        {
            throw new ReglaNegocioException("El cajero ya está asignado a esta caja.");
        }

        if (CajerosActivos >= MaximoCajeros)
        {
            throw new ReglaNegocioException("Cada caja admite como máximo 2 cajeros.");
        }

        Asignaciones.Add(new AsignacionCaja
        {
            CajaId = Id,
            UsuarioId = cajeroId,
            AsignadoPorId = gestorId,
            Activa = true
        });
        Tocar();
    }

    public void QuitarCajero(int asignacionId)
    {
        var asignacion = Asignaciones.FirstOrDefault(a => a.Id == asignacionId && !a.Eliminado)
            ?? throw new ReglaNegocioException("La asignación no existe.");

        if (UsuarioActivoId == asignacion.UsuarioId)
        {
            throw new ReglaNegocioException("No se puede quitar al cajero que tiene la caja abierta.");
        }

        asignacion.Activa = false;
        asignacion.MarcarEliminado();
        Tocar();
    }

    public void AbrirSesion(int cajeroId)
    {
        if (Estado != EstadoCaja.Activa || Eliminado)
        {
            throw new ReglaNegocioException("La caja no está activa.");
        }

        var asignado = Asignaciones.Any(a => a.Activa && !a.Eliminado && a.UsuarioId == cajeroId);
        if (!asignado)
        {
            throw new ReglaNegocioException("El cajero no está asignado a esta caja.");
        }

        if (UsuarioActivoId.HasValue && UsuarioActivoId != cajeroId)
        {
            throw new ReglaNegocioException("Otro cajero ya está operando esta caja. Los dos asignados no pueden atender al mismo tiempo.");
        }

        UsuarioActivoId = cajeroId;
        FechaAperturaSesion = DateTime.UtcNow;
        Tocar();
    }

    public void CerrarSesion(int cajeroId)
    {
        if (UsuarioActivoId != cajeroId)
        {
            throw new ReglaNegocioException("No tiene una sesión abierta en esta caja.");
        }

        UsuarioActivoId = null;
        FechaAperturaSesion = null;
        Tocar();
    }
}
