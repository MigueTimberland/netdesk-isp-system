namespace NetDesk.Domain.Entities;

public class TokenRecuperacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public DateTime? UsadoEn { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool Vigente => UsadoEn is null && ExpiraEn > DateTime.UtcNow;

    public void MarcarUsado()
    {
        UsadoEn = DateTime.UtcNow;
    }
}
