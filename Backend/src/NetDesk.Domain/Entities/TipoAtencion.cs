namespace NetDesk.Domain.Entities;

public class TipoAtencion
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
