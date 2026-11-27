namespace NetDesk.Domain.Common;

public abstract class EntidadAuditable
{
    public int Id { get; set; }
    public bool Eliminado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public void MarcarEliminado()
    {
        if (Eliminado)
        {
            return;
        }

        Eliminado = true;
        FechaEliminacion = DateTime.UtcNow;
        FechaModificacion = DateTime.UtcNow;
    }

    public void Tocar()
    {
        FechaModificacion = DateTime.UtcNow;
    }
}
