using System.ComponentModel.DataAnnotations;

namespace EnergyBackend.Data;

public class Casa
{
    public int Id { get; set; }
    public required string CantidadPersonas { get; set; }
    public bool IsComplete { get; set; }
    [StringLength(100)]
    public string? NumeroContrato { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public Direccion? Direccion { get; set; }
    public ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}

public class Direccion
{
    public int Id { get; set; }
    public required string Calle { get; set; }
    public required string Ciudad { get; set; }
    public required string Estado { get; set; }
    public required string CodigoPostal { get; set; }

    public int? CasaId { get; set; }
    public Casa? Casa { get; set; }
}

public class Usuario
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string CorreoElectronico { get; set; }
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public ICollection<Casa> Casas { get; set; } = new List<Casa>();
    public ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}

public class Factura
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    // Nullable para permitir migrar facturas antiguas antes de asignarlas a una casa.
    public int? CasaId { get; set; }
    public Casa? Casa { get; set; }

    [Required, StringLength(100)]
    public required string NumeroContrato { get; set; }

    public DateTime FechaEmision { get; set; }
    public DateTime? FechaPago { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoAcueductoM3 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoAlcantarilladoM3 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoEnergiaKwh { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoGasM3 { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
