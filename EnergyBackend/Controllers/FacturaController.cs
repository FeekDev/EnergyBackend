using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnergyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FacturaController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FacturaResponse>>> GetFacturas(CancellationToken cancellationToken)
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var facturas = await context.Facturas
            .AsNoTracking()
            .Where(factura => factura.UsuarioId == usuarioId.Value)
            .OrderByDescending(factura => factura.FechaEmision)
            .Select(factura => new FacturaResponse(
                factura.Id,
                factura.CasaId,
                factura.NumeroContrato,
                factura.FechaEmision,
                factura.FechaPago,
                factura.ConsumoAcueductoM3,
                factura.ConsumoAlcantarilladoM3,
                factura.ConsumoEnergiaKwh,
                factura.ConsumoGasM3))
            .ToListAsync(cancellationToken);

        return Ok(facturas);
    }

    [HttpPost]
    public async Task<ActionResult<FacturaResponse>> RegistrarFactura(
        RegistrarFacturaRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (!await context.Casas.AnyAsync(casa =>
                casa.Id == request.CasaId && casa.UsuarioId == usuarioId.Value,
                cancellationToken))
        {
            return BadRequest(new { message = "La casa no existe o no pertenece al usuario autenticado." });
        }

        if (request.FechaPago is not null && request.FechaPago.Value < request.FechaEmision)
        {
            ModelState.AddModelError(nameof(request.FechaPago),
                "La fecha de pago no puede ser anterior a la fecha de emisión.");
            return ValidationProblem(ModelState);
        }

        var factura = new Factura
        {
            UsuarioId = usuarioId.Value,
            CasaId = request.CasaId,
            NumeroContrato = request.NumeroContrato.Trim(),
            FechaEmision = request.FechaEmision,
            FechaPago = request.FechaPago,
            ConsumoAcueductoM3 = request.ConsumoAcueductoM3,
            ConsumoAlcantarilladoM3 = request.ConsumoAlcantarilladoM3,
            ConsumoEnergiaKwh = request.ConsumoEnergiaKwh,
            ConsumoGasM3 = request.ConsumoGasM3
        };

        context.Facturas.Add(factura);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetFacturas), null, ToResponse(factura));
    }

    [HttpGet("recomendaciones-trimestrales")]
    public async Task<ActionResult<IReadOnlyList<RecomendacionTrimestralResponse>>> GetRecomendacionesTrimestrales(
        CancellationToken cancellationToken)
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var registros = await context.Facturas
            .AsNoTracking()
            .Where(factura => factura.UsuarioId == usuarioId.Value && factura.CasaId != null)
            .Select(factura => new
            {
                CasaId = factura.CasaId!.Value,
                factura.FechaEmision,
                factura.ConsumoEnergiaKwh,
                factura.ConsumoAcueductoM3,
                factura.ConsumoAlcantarilladoM3,
                factura.ConsumoGasM3
            })
            .ToListAsync(cancellationToken);

        var resultados = new List<RecomendacionTrimestralResponse>();
        foreach (var facturasCasa in registros.GroupBy(factura => factura.CasaId))
        {
            var periodos = facturasCasa
                .GroupBy(factura => (
                    Anio: factura.FechaEmision.Year,
                    Trimestre: (factura.FechaEmision.Month - 1) / 3 + 1))
                .Select(grupo => new ConsumoPeriodo(
                    grupo.Key.Anio,
                    grupo.Key.Trimestre,
                    grupo.Sum(factura => factura.ConsumoEnergiaKwh),
                    grupo.Sum(factura => factura.ConsumoAcueductoM3),
                    grupo.Sum(factura => factura.ConsumoAlcantarilladoM3),
                    grupo.Sum(factura => factura.ConsumoGasM3)))
                .OrderByDescending(periodo => periodo.Anio)
                .ThenByDescending(periodo => periodo.Trimestre)
                .ToList();

            var actual = periodos[0];
            var trimestreAnterior = actual.Trimestre == 1
                ? (actual.Anio - 1, 4)
                : (actual.Anio, actual.Trimestre - 1);
            var anterior = periodos.FirstOrDefault(periodo =>
                periodo.Anio == trimestreAnterior.Item1 &&
                periodo.Trimestre == trimestreAnterior.Item2);

            resultados.Add(new RecomendacionTrimestralResponse(
                facturasCasa.Key,
                actual.Anio,
                actual.Trimestre,
                CrearConsumoRecomendado(
                    "Energía", "kWh", actual.ConsumoEnergiaKwh, anterior?.ConsumoEnergiaKwh,
                    "Revisa iluminación, equipos en espera y el uso de equipos de alto consumo."),
                CrearConsumoRecomendado(
                    "Acueducto", "m³", actual.ConsumoAcueductoM3, anterior?.ConsumoAcueductoM3,
                    "Revisa posibles fugas y prioriza duchas más cortas y dispositivos ahorradores."),
                CrearConsumoRecomendado(
                    "Alcantarillado", "m³", actual.ConsumoAlcantarilladoM3, anterior?.ConsumoAlcantarilladoM3,
                    "Reduce el agua desechada reutilizando agua cuando sea seguro y evita descargas innecesarias."),
                CrearConsumoRecomendado(
                    "Gas", "m³", actual.ConsumoGasM3, anterior?.ConsumoGasM3,
                    "Revisa la eficiencia de los equipos a gas, la ventilación y su mantenimiento periódico.")));
        }

        return Ok(resultados);
    }

    private int? GetUsuarioId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
            ? usuarioId
            : null;
    }

    private static FacturaResponse ToResponse(Factura factura) => new(
        factura.Id,
        factura.CasaId,
        factura.NumeroContrato,
        factura.FechaEmision,
        factura.FechaPago,
        factura.ConsumoAcueductoM3,
        factura.ConsumoAlcantarilladoM3,
        factura.ConsumoEnergiaKwh,
        factura.ConsumoGasM3);

    private static ConsumoRecomendacionResponse CrearConsumoRecomendado(
        string servicio,
        string unidad,
        decimal actual,
        decimal? anterior,
        string consejoAhorro)
    {
        if (anterior is null)
        {
            return new ConsumoRecomendacionResponse(
                servicio,
                unidad,
                actual,
                null,
                null,
                $"Este es el primer trimestre disponible. Registra cada factura para establecer una referencia. {consejoAhorro}");
        }

        decimal? variacion = anterior.Value == 0
            ? null
            : Math.Round((actual - anterior.Value) / anterior.Value * 100m, 2);

        var recomendacion = actual > anterior.Value
            ? anterior.Value == 0
                ? $"El consumo pasó de cero a {actual} {unidad}. Verifica la lectura y los equipos asociados. {consejoAhorro}"
                : $"El consumo aumentó {variacion}% frente al trimestre anterior. {consejoAhorro}"
            : actual < anterior.Value
                ? $"El consumo disminuyó {Math.Abs(variacion ?? 0)}% frente al trimestre anterior. Mantén las prácticas que ayudaron a reducirlo."
                : $"El consumo se mantuvo estable frente al trimestre anterior. {consejoAhorro}";

        return new ConsumoRecomendacionResponse(
            servicio,
            unidad,
            actual,
            anterior,
            variacion,
            recomendacion);
    }

    private sealed record ConsumoPeriodo(
        int Anio,
        int Trimestre,
        decimal ConsumoEnergiaKwh,
        decimal ConsumoAcueductoM3,
        decimal ConsumoAlcantarilladoM3,
        decimal ConsumoGasM3);
}

public sealed class RegistrarFacturaRequest
{
    [Range(1, int.MaxValue)]
    public int CasaId { get; init; }

    [Required, StringLength(100)]
    public string NumeroContrato { get; init; } = string.Empty;

    public DateTime FechaEmision { get; init; }
    public DateTime? FechaPago { get; init; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoAcueductoM3 { get; init; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoAlcantarilladoM3 { get; init; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoEnergiaKwh { get; init; }

    [Range(0, double.MaxValue)]
    public decimal ConsumoGasM3 { get; init; }
}

public sealed record FacturaResponse(
    int Id,
    int? CasaId,
    string NumeroContrato,
    DateTime FechaEmision,
    DateTime? FechaPago,
    decimal ConsumoAcueductoM3,
    decimal ConsumoAlcantarilladoM3,
    decimal ConsumoEnergiaKwh,
    decimal ConsumoGasM3);

/// <summary>Consumo de un servicio y recomendación basada en el trimestre más reciente.</summary>
public sealed record ConsumoRecomendacionResponse(
    string Servicio,
    string Unidad,
    decimal ConsumoActual,
    decimal? ConsumoTrimestreAnterior,
    decimal? VariacionPorcentual,
    string Recomendacion);

/// <summary>Recomendaciones trimestrales para una casa.</summary>
public sealed record RecomendacionTrimestralResponse(
    int CasaId,
    int Anio,
    int Trimestre,
    ConsumoRecomendacionResponse Energia,
    ConsumoRecomendacionResponse Acueducto,
    ConsumoRecomendacionResponse Alcantarillado,
    ConsumoRecomendacionResponse Gas);