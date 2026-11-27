namespace NetDesk.Domain.Entities;

public class FormaPago
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}
