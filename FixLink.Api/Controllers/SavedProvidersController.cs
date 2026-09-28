using System.Security.Claims;
using FixLink.Domain.Entities;
using FixLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SavedProvidersController : ControllerBase
{
    private readonly FixLinkDbContext _context;

    public SavedProvidersController(FixLinkDbContext context)
    {
        _context = context;
    }

    // GET: api/savedproviders
    // Gets the saved providers for the logged-in user.
    [HttpGet]
    public async Task<IActionResult> GetSavedProviders()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var savedProviders = await _context.SavedProviders
            .AsNoTracking()
            .Where(sp => sp.UserId == userId.Value)
            .Where(sp => sp.Provider.IsActive &&
                         sp.Provider.IsPublished)
            .Include(sp => sp.Provider)
            .OrderByDescending(sp => sp.SavedAt)
            .Select(sp => new
            {
                sp.ProviderId,
                sp.SavedAt,
                Provider = new
                {
                    sp.Provider.Id,
                    sp.Provider.BusinessName,
                    sp.Provider.Description,
                    sp.Provider.PhoneNumber,
                    sp.Provider.WhatsAppNumber,
                    sp.Provider.City,
                    sp.Provider.Province
                }
            })
            .ToListAsync();

        return Ok(savedProviders);
    }

    // POST: api/savedproviders/{providerId}
    // Saves a provider for the logged-in user.
    [HttpPost("{providerId:guid}")]
    public async Task<IActionResult> SaveProvider(Guid providerId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var provider = await _context.Providers
            .FirstOrDefaultAsync(p =>
                p.Id == providerId &&
                p.IsActive &&
                p.IsPublished);

        if (provider == null)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var alreadySaved = await _context.SavedProviders
            .AnyAsync(sp =>
                sp.UserId == userId.Value &&
                sp.ProviderId == providerId);

        if (alreadySaved)
        {
            return Conflict(new
            {
                message = "You have already saved this provider."
            });
        }

        var savedProvider = new SavedProvider
        {
            UserId = userId.Value,
            ProviderId = providerId,
            SavedAt = DateTime.UtcNow
        };

        _context.SavedProviders.Add(savedProvider);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSavedProviders),
            new
            {
                providerId = providerId
            },
            new
            {
                message = "Provider saved successfully.",
                savedProvider.UserId,
                savedProvider.ProviderId,
                savedProvider.SavedAt
            });
    }

    // DELETE: api/savedproviders/{providerId}
    // Removes a saved provider for the logged-in user.
    [HttpDelete("{providerId:guid}")]
    public async Task<IActionResult> RemoveSavedProvider(
        Guid providerId)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var savedProvider = await _context.SavedProviders
            .FirstOrDefaultAsync(sp =>
                sp.UserId == userId.Value &&
                sp.ProviderId == providerId);

        if (savedProvider == null)
        {
            return NotFound(new
            {
                message = "Saved provider not found."
            });
        }

        _context.SavedProviders.Remove(savedProvider);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Provider removed from saved providers."
        });
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userId, out var parsedUserId))
            return parsedUserId;

        return null;
    }
}