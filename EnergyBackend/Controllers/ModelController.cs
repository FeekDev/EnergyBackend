using EnergyBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EnergyBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CasaController : ControllerBase
{
    private readonly AppDbContext _context;
    public CasaController(AppDbContext context)
    {
        _context = context;
    }
    [HttpGet(Name = "GetCasa")]
    public async Task<ActionResult<IEnumerable<Casa>>> GetCasas()
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        return await _context.Casas
            .AsNoTracking()
            .Include(casa => casa.Direccion)
            .Where(casa => casa.UsuarioId == usuarioId.Value)
            .ToListAsync();
    }

    [HttpPost(Name = "PostCasa")]
    public async Task<ActionResult<Casa>> PostCasa(CasaRequest request)
    {
        var usuarioId = GetUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var casa = new Casa
        {
            CantidadPersonas = request.CantidadPersonas,
            UsuarioId = usuarioId.Value
        };

        _context.Casas.Add(casa);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCasas), new { id = casa.Id }, casa);
    }

    private int? GetUsuarioId()
    {
        return int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var usuarioId)
            ? usuarioId
            : null;
    }
}

public sealed class CasaRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string CantidadPersonas { get; init; } = string.Empty;
}

