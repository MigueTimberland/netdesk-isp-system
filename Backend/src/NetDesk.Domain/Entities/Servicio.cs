using NetDesk.Domain.Common;

namespace NetDesk.Domain.Entities;

public class Servicio : EntidadAuditable
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Equipos { get; set; } = string.Empty;
    public int VelocidadMbps { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}
