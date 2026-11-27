using NetDesk.Domain.Common;

namespace NetDesk.Domain.Entities;

public class Pago : EntidadAuditable
{
    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public int FormaPagoId { get; set; }
    public FormaPago FormaPago { get; set; } = null!;
    public int? TurnoId { get; set; }
    public Turno? Turno { get; set; }
    public int CajeroId { get; set; }
    public Usuario Cajero { get; set; } = null!;
    public string? Referencia { get; set; }
}
