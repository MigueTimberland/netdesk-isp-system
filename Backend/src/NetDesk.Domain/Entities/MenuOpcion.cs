namespace NetDesk.Domain.Entities;

public class MenuOpcion
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public int Orden { get; set; }
    public int? PadreId { get; set; }
    public bool Activo { get; set; } = true;
}
