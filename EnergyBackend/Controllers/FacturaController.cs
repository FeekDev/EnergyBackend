using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using EnergyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FacturaController(
    AppDbContext context,
    IWebHostEnvironment environment) : ControllerBase
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

        var numeroContrato = request.NumeroContrato.Trim();
        Casa? casa;
        if (request.CasaId is not null)
        {
            casa = await context.Casas.SingleOrDefaultAsync(
                item => item.Id == request.CasaId && item.UsuarioId == usuarioId.Value,
                cancellationToken);
            if (casa is null)
            {
                return BadRequest(new { message = "La casa no existe o no pertenece al usuario autenticado." });
            }

            if (casa.NumeroContrato is not null && casa.NumeroContrato != numeroContrato)
            {
                return BadRequest(new { message = "La casa ya está asociada a otro número de contrato." });
            }
        }
        else
        {
            casa = await context.Casas.SingleOrDefaultAsync(
                item => item.UsuarioId == usuarioId.Value && item.NumeroContrato == numeroContrato,
                cancellationToken);

            if (casa is null)
            {
                if (string.IsNullOrWhiteSpace(request.CantidadPersonas))
                {
                    ModelState.AddModelError(
                        nameof(request.CantidadPersonas),
                        "Indica la cantidad de personas para crear la casa asociada al contrato.");
                    return ValidationProblem(ModelState);
                }

                casa = new Casa
                {
                    NumeroContrato = numeroContrato,
                    CantidadPersonas = request.CantidadPersonas.Trim(),
                    UsuarioId = usuarioId.Value
                };
                context.Casas.Add(casa);
            }
        }

        var contratoEnOtraCasa = await context.Casas.AnyAsync(
            item => item.UsuarioId == usuarioId.Value
                && item.NumeroContrato == numeroContrato
                && item.Id != casa.Id,
            cancellationToken);
        if (contratoEnOtraCasa)
        {
            return Conflict(new { message = "El número de contrato ya está asociado a otra casa." });
        }

        casa.NumeroContrato = numeroContrato;

        if (request.FechaPago is not null && request.FechaPago.Value < request.FechaEmision)
        {
            ModelState.AddModelError(nameof(request.FechaPago),
                "La fecha de pago no puede ser anterior a la fecha de emisión.");
            return ValidationProblem(ModelState);
        }

        var factura = new Factura
        {
            UsuarioId = usuarioId.Value,
            Casa = casa,
            NumeroContrato = numeroContrato,
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

    [HttpPost("datos-demo")]
    public async Task<ActionResult<CargaDemoResponse>> CargarDatosDemo(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        const string numeroContrato = "2077739";
        var consumosMensuales = new (decimal Energia, decimal Agua, decimal Gas)[]
        {
            (245, 14.2m, 11.4m), (238, 13.8m, 10.9m), (251, 14.5m, 11.1m),
            (232, 13.9m, 10.6m), (225, 13.4m, 10.2m), (229, 13.7m, 10.4m),
            (221, 13.1m, 9.9m), (218, 12.9m, 9.8m), (220, 13.2m, 10m),
            (215, 12.7m, 9.5m), (211, 12.5m, 9.2m), (214, 12.8m, 9.4m),
            (207, 12.3m, 9m), (202, 12.1m, 8.8m), (205, 12.4m, 8.9m),
            (198, 11.9m, 8.6m), (195, 11.7m, 8.4m), (197, 11.8m, 8.5m),
            (190, 11.4m, 8.2m), (187, 11.2m, 8m), (188, 11.3m, 8.1m)
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var casa = await context.Casas.SingleOrDefaultAsync(
            item => item.UsuarioId == usuarioId.Value && item.NumeroContrato == numeroContrato,
            cancellationToken);
        if (casa is null)
        {
            casa = new Casa
            {
                NumeroContrato = numeroContrato,
                CantidadPersonas = "3",
                UsuarioId = usuarioId.Value
            };
            context.Casas.Add(casa);
            await context.SaveChangesAsync(cancellationToken);
        }

        var facturasExistentes = await context.Facturas
            .Where(factura =>
                factura.UsuarioId == usuarioId.Value &&
                factura.NumeroContrato == numeroContrato &&
                factura.FechaEmision >= new DateTime(2025, 1, 1) &&
                factura.FechaEmision < new DateTime(2026, 10, 1))
            .Select(factura => factura.FechaEmision)
            .ToListAsync(cancellationToken);
        var mesesExistentes = facturasExistentes
            .Select(fecha => (fecha.Year, fecha.Month))
            .ToHashSet();
        var facturasNuevas = new List<Factura>();

        for (var index = 0; index < consumosMensuales.Length; index++)
        {
            var fechaEmision = new DateTime(2025, 1, 15).AddMonths(index);
            if (mesesExistentes.Contains((fechaEmision.Year, fechaEmision.Month)))
            {
                continue;
            }

            var consumo = consumosMensuales[index];
            facturasNuevas.Add(new Factura
            {
                UsuarioId = usuarioId.Value,
                CasaId = casa.Id,
                NumeroContrato = numeroContrato,
                FechaEmision = fechaEmision,
                ConsumoEnergiaKwh = consumo.Energia,
                ConsumoAcueductoM3 = consumo.Agua,
                ConsumoAlcantarilladoM3 = consumo.Agua,
                ConsumoGasM3 = consumo.Gas
            });
        }

        context.Facturas.AddRange(facturasNuevas);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new CargaDemoResponse(
            numeroContrato,
            facturasNuevas.Count,
            mesesExistentes.Count,
            consumosMensuales.Length,
            "Datos ficticios de demostración; no representan lecturas reales."));
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
                    grupo.Sum(factura => factura.ConsumoGasM3),
                    grupo.Count()))
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
            var hoy = DateTime.UtcNow;
            var esTrimestreEnCurso = actual.Anio == hoy.Year &&
                actual.Trimestre == (hoy.Month - 1) / 3 + 1;
            var periodoParaComparar = esTrimestreEnCurso ? null : anterior;

            resultados.Add(new RecomendacionTrimestralResponse(
                facturasCasa.Key,
                actual.Anio,
                actual.Trimestre,
                actual.CantidadFacturas,
                esTrimestreEnCurso,
                CrearConsumoRecomendado(
                    "Energía", "kWh", actual.ConsumoEnergiaKwh, periodoParaComparar?.ConsumoEnergiaKwh,
                    esTrimestreEnCurso,
                    "Revisa iluminación, equipos en espera y el uso de equipos de alto consumo."),
                CrearConsumoRecomendado(
                    "Acueducto", "m³", actual.ConsumoAcueductoM3, periodoParaComparar?.ConsumoAcueductoM3,
                    esTrimestreEnCurso,
                    "Revisa posibles fugas y prioriza duchas más cortas y dispositivos ahorradores."),
                CrearConsumoRecomendado(
                    "Alcantarillado", "m³", actual.ConsumoAlcantarilladoM3, periodoParaComparar?.ConsumoAlcantarilladoM3,
                    esTrimestreEnCurso,
                    "Reduce el agua desechada reutilizando agua cuando sea seguro y evita descargas innecesarias."),
                CrearConsumoRecomendado(
                    "Gas", "m³", actual.ConsumoGasM3, periodoParaComparar?.ConsumoGasM3,
                    esTrimestreEnCurso,
                    "Revisa la eficiencia de los equipos a gas, la ventilación y su mantenimiento periódico.")));
        }

        return Ok(resultados);
    }

    [HttpGet("linea-tiempo/{numeroContrato}.svg")]
    [Produces("image/svg+xml")]
    public async Task<IActionResult> GetLineaTiempoConsumos(
        string numeroContrato,
        CancellationToken cancellationToken)
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var registros = await context.Facturas
            .AsNoTracking()
            .Where(factura =>
                factura.UsuarioId == usuarioId.Value &&
                factura.NumeroContrato == numeroContrato)
            .Select(factura => new
            {
                factura.FechaEmision,
                factura.ConsumoEnergiaKwh,
                factura.ConsumoAcueductoM3,
                factura.ConsumoAlcantarilladoM3,
                factura.ConsumoGasM3
            })
            .ToListAsync(cancellationToken);

        var periodos = registros
            .GroupBy(factura => (
                Anio: factura.FechaEmision.Year,
                Trimestre: (factura.FechaEmision.Month - 1) / 3 + 1))
            .Select(grupo => new ConsumoGrafico(
                grupo.Key.Anio,
                grupo.Key.Trimestre,
                grupo.Sum(factura => factura.ConsumoEnergiaKwh),
                grupo.Sum(factura => factura.ConsumoAcueductoM3),
                grupo.Sum(factura => factura.ConsumoAlcantarilladoM3),
                grupo.Sum(factura => factura.ConsumoGasM3)))
            .OrderBy(periodo => periodo.Anio)
            .ThenBy(periodo => periodo.Trimestre)
            .TakeLast(8)
            .ToList();

        if (periodos.Count == 0)
        {
            return NotFound(new { message = "No hay facturas para este contrato." });
        }

        return Content(
            CrearGraficoSvg(numeroContrato, periodos),
            "image/svg+xml; charset=utf-8");
    }

    private static string CrearGraficoSvg(string numeroContrato, IReadOnlyList<ConsumoGrafico> periodos)
    {
        const int ancho = 900;
        const int altoPanel = 145;
        const int margenIzquierdo = 110;
        const int margenDerecho = 35;
        const int alto = 90 + altoPanel * 4;
        var anchoGrafico = ancho - margenIzquierdo - margenDerecho;
        var etiquetas = periodos
            .Select(periodo => $"T{periodo.Trimestre} {periodo.Anio}")
            .ToArray();
        var series = new[]
        {
            new SerieGrafico("Energía", "kWh", "#d97706", periodos.Select(item => item.ConsumoEnergiaKwh).ToArray()),
            new SerieGrafico("Acueducto", "m³", "#0284c7", periodos.Select(item => item.ConsumoAcueductoM3).ToArray()),
            new SerieGrafico("Alcantarillado", "m³", "#7c3aed", periodos.Select(item => item.ConsumoAlcantarilladoM3).ToArray()),
            new SerieGrafico("Gas", "m³", "#059669", periodos.Select(item => item.ConsumoGasM3).ToArray())
        };
        var sb = new StringBuilder();
        var contratoSeguro = WebUtility.HtmlEncode(numeroContrato);

        sb.Append(CultureInfo.InvariantCulture, $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{ancho}" height="{alto}" viewBox="0 0 {ancho} {alto}" role="img" aria-labelledby="titulo descripcion">
            """);
        sb.Append("""
            <style>
              text { font-family: Arial, sans-serif; fill: #1f2937; }
              .titulo { font-size: 22px; font-weight: 700; }
              .subtitulo { font-size: 13px; fill: #4b5563; }
              .servicio { font-size: 15px; font-weight: 700; }
              .eje { font-size: 11px; fill: #4b5563; }
              .rejilla { stroke: #e5e7eb; stroke-width: 1; }
            </style>
            """);
        sb.Append(CultureInfo.InvariantCulture, $"""
              <title id="titulo">Consumo trimestral del contrato {contratoSeguro}</title>
              <desc id="descripcion">Gráfico de línea con hasta ocho trimestres. Cada servicio tiene su propia escala para respetar sus unidades.</desc>
              <rect width="100%" height="100%" fill="#ffffff"/>
              <text class="titulo" x="{margenIzquierdo}" y="30">Consumo del contrato {contratoSeguro}</text>
              <text class="subtitulo" x="{margenIzquierdo}" y="52">Totales por trimestre · máximo ocho trimestres · escala independiente por servicio</text>
            """);

        for (var serieIndex = 0; serieIndex < series.Length; serieIndex++)
        {
            var serie = series[serieIndex];
            var panelTop = 72 + serieIndex * altoPanel;
            var graficoTop = panelTop + 18;
            var graficoAlto = 85;
            var graficoAbajo = graficoTop + graficoAlto;
            var minimo = serie.Valores.Min();
            var maximo = serie.Valores.Max();
            var rango = maximo - minimo;
            var relleno = rango == 0 ? Math.Max(Math.Abs(maximo) * 0.1m, 1m) : rango * 0.1m;
            var limiteInferior = Math.Max(0, minimo - relleno);
            var limiteSuperior = maximo + relleno;
            var rangoVisible = limiteSuperior - limiteInferior;

            sb.Append(CultureInfo.InvariantCulture, $"""
                  <text class="servicio" x="16" y="{panelTop + 22}" fill="{serie.Color}">{serie.Nombre} ({serie.Unidad})</text>
                """);

            for (var tick = 0; tick <= 2; tick++)
            {
                var proporcion = tick / 2m;
                var y = graficoAbajo - (double)(proporcion * graficoAlto);
                var etiqueta = (limiteInferior + proporcion * rangoVisible)
                    .ToString("0.##", CultureInfo.InvariantCulture);

                sb.Append(CultureInfo.InvariantCulture, $"""
                      <line class="rejilla" x1="{margenIzquierdo}" y1="{y:0.##}" x2="{ancho - margenDerecho}" y2="{y:0.##}"/>
                      <text class="eje" text-anchor="end" x="{margenIzquierdo - 8}" y="{y + 4:0.##}">{etiqueta}</text>
                    """);
            }

            var puntos = serie.Valores
                .Select((valor, index) =>
                {
                    var x = periodos.Count == 1
                        ? margenIzquierdo + anchoGrafico / 2d
                        : margenIzquierdo + index * anchoGrafico / (double)(periodos.Count - 1);
                    var y = graficoAbajo - (double)((valor - limiteInferior) / rangoVisible * graficoAlto);
                    return (X: x, Y: y, Valor: valor, Etiqueta: etiquetas[index]);
                })
                .ToArray();
            var puntosSvg = string.Join(" ", puntos.Select(punto => $"{punto.X:0.##},{punto.Y:0.##}"));

            sb.Append(CultureInfo.InvariantCulture, $"""
                  <polyline fill="none" stroke="{serie.Color}" stroke-width="3" stroke-linejoin="round" stroke-linecap="round" points="{puntosSvg}"/>
                """);

            foreach (var punto in puntos)
            {
                var etiquetaValor = punto.Valor.ToString("0.##", CultureInfo.InvariantCulture);
                var etiquetaTrimestre = WebUtility.HtmlEncode(punto.Etiqueta);
                sb.Append(CultureInfo.InvariantCulture, $"""
                      <circle cx="{punto.X:0.##}" cy="{punto.Y:0.##}" r="4" fill="{serie.Color}">
                        <title>{etiquetaTrimestre}: {etiquetaValor} {serie.Unidad}</title>
                      </circle>
                      <text class="eje" text-anchor="middle" x="{punto.X:0.##}" y="{graficoAbajo + 18}">{etiquetaTrimestre}</text>
                    """);
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
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
        bool esTrimestreEnCurso,
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
                esTrimestreEnCurso
                    ? $"Recomendación preliminar basada en las facturas registradas hasta ahora. Se actualizará al registrar las siguientes facturas del trimestre. {consejoAhorro}"
                    : $"No hay datos del trimestre anterior para comparar. Registra las facturas para establecer una referencia. {consejoAhorro}");
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
        decimal ConsumoGasM3,
        int CantidadFacturas);

    private sealed record ConsumoGrafico(
        int Anio,
        int Trimestre,
        decimal ConsumoEnergiaKwh,
        decimal ConsumoAcueductoM3,
        decimal ConsumoAlcantarilladoM3,
        decimal ConsumoGasM3);

    private sealed record SerieGrafico(
        string Nombre,
        string Unidad,
        string Color,
        decimal[] Valores);
}

public sealed class RegistrarFacturaRequest
{
    [Range(1, int.MaxValue)]
    public int? CasaId { get; init; }

    [Required, StringLength(100)]
    public string NumeroContrato { get; init; } = string.Empty;

    [StringLength(100)]
    public string? CantidadPersonas { get; init; }

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
    int CantidadFacturas,
    bool EsPreliminar,
    ConsumoRecomendacionResponse Energia,
    ConsumoRecomendacionResponse Acueducto,
    ConsumoRecomendacionResponse Alcantarillado,
    ConsumoRecomendacionResponse Gas);

/// <summary>Resultado de la carga idempotente de facturas ficticias de demostración.</summary>
public sealed record CargaDemoResponse(
    string NumeroContrato,
    int FacturasAgregadas,
    int MesesOmitidos,
    int TotalMesesDemostracion,
    string Aviso);