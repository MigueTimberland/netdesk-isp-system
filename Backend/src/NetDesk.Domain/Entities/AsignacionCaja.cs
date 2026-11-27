using NetDesk.Domain.Common;

namespace NetDesk.Domain.Entities;

public class AsignacionCaja : EntidadAuditable
{
    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int AsignadoPorId { get; set; }
    public Usuario AsignadoPor { get; set; } = null!;
    public bool Activa { get; set; } = true;
}
