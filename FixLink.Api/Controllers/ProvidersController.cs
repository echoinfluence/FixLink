using FixLink.Application.DTOs.Provider;
using FixLink.Domain.Entities;
using FixLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly FixLinkDbContext _context;

    public ProvidersController(FixLinkDbContext context)
    {
        _context = context;
    }

    // GET: api/providers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProviderResponse>>> GetProviders(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? serviceId = null,
        [FromQuery] string? city = null,
        [FromQuery] string? province = null,
        [FromQuery] bool? verified = null,
        [FromQuery] string? sort = null)
    {
        var query = _context.Providers
            .AsNoTracking()
            .Where(p => p.IsActive && p.IsPublished);

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();

            query = query.Where(p =>
                p.BusinessName.Contains(searchTerm) ||
                p.Description.Contains(searchTerm) ||
                p.Suburb.Contains(searchTerm) ||
                p.City.Contains(searchTerm) ||
                p.Province.Contains(searchTerm) ||
                p.ProviderServices.Any(ps =>
                    ps.Service.Name.Contains(searchTerm)));
        }

        // Filter by service category
        if (categoryId.HasValue)
        {
            query = query.Where(p =>
                p.ProviderServices.Any(ps =>
                    ps.Service.ServiceCategoryId == categoryId.Value));
        }

        // Filter by specific service
        if (serviceId.HasValue)
        {
            query = query.Where(p =>
                p.ProviderServices.Any(ps =>
                    ps.ServiceId == serviceId.Value));
        }

        // Filter by city
        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityFilter = city.Trim();

            query = query.Where(p =>
                p.City.Contains(cityFilter));
        }

        // Filter by province
        if (!string.IsNullOrWhiteSpace(province))
        {
            var provinceFilter = province.Trim();

            query = query.Where(p =>
                p.Province.Contains(provinceFilter));
        }

        // Filter by verified status
        if (verified.HasValue)
        {
            query = query.Where(p =>
                p.IsVerified == verified.Value);
        }

        // Sorting
        query = sort?.ToLowerInvariant() switch
        {
            "verified" =>
                query
                    .OrderByDescending(p => p.IsVerified)
                    .ThenBy(p => p.BusinessName),

            "newest" =>
                query
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.BusinessName),

            "oldest" =>
                query
                    .OrderBy(p => p.CreatedAt)
                    .ThenBy(p => p.BusinessName),

            _ =>
                query.OrderBy(p => p.BusinessName)
        };

        var providers = await query
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.BusinessHours)
            .ToListAsync();

        var response = providers.Select(MapToResponse);

        return Ok(response);
    }



    // GET: api/providers/admin
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<ProviderResponse>>>
        GetAllProvidersForAdmin()
    {
        var providers = await _context.Providers
            .AsNoTracking()
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.BusinessHours)
            .OrderBy(p => p.BusinessName)
            .ToListAsync();

        var response = providers.Select(MapToResponse);

        return Ok(response);
    }



    // GET: api/providers/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProviderResponse>> GetProvider(Guid id)
    {
        var provider = await _context.Providers
            .AsNoTracking()
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.ServiceCategory)
                    .Include(p => p.BusinessHours)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.IsActive &&
                p.IsPublished);

        if (provider == null)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        return Ok(MapToResponse(provider));
    }

    // POST: api/providers
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProviderResponse>> CreateProvider(
        CreateProviderRequest request)
    {
        if (request.ServiceIds == null ||
            request.ServiceIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one service is required."
            });
        }

        var serviceIds = request.ServiceIds.Distinct().ToList();

        var services = await _context.Services
            .Where(s =>
                serviceIds.Contains(s.Id) &&
                s.IsActive)
            .ToListAsync();

        if (services.Count != serviceIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more service IDs are invalid."
            });
        }

        var provider = new Provider
        {
            Id = Guid.NewGuid(),

            BusinessName = request.BusinessName.Trim(),
            Description = request.Description.Trim(),

            PhoneNumber = request.PhoneNumber.Trim(),
            WhatsAppNumber = request.WhatsAppNumber?.Trim(),
            Email = request.Email?.Trim(),
            Website = request.Website?.Trim(),

            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            Suburb = request.Suburb.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            PostalCode = request.PostalCode.Trim(),

            Latitude = request.Latitude,
            Longitude = request.Longitude,

            YearsInBusiness = request.YearsInBusiness,

            IsVerified = request.IsVerified,
            IsPublished = true,
            IsActive = true,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var service in services)
        {
            provider.ProviderServices.Add(
                new ProviderService
                {
                    ProviderId = provider.Id,
                    ServiceId = service.Id
                });
        }

        _context.Providers.Add(provider);

        await _context.SaveChangesAsync();

        await _context.Entry(provider)
            .Collection(p => p.ProviderServices)
            .Query()
            .Include(ps => ps.Service)
                .ThenInclude(s => s.ServiceCategory)
            .LoadAsync();

        var response = MapToResponse(provider);

        return CreatedAtAction(
            nameof(GetProvider),
            new { id = provider.Id },
            response);
    }

    // PUT: api/providers/{id}
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProviderResponse>> UpdateProvider(
        Guid id,
        UpdateProviderRequest request)
    {
        var provider = await _context.Providers
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var serviceIds = request.ServiceIds
            .Distinct()
            .ToList();

        if (serviceIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one service is required."
            });
        }

        var services = await _context.Services
            .Where(s =>
                serviceIds.Contains(s.Id) &&
                s.IsActive)
            .ToListAsync();

        if (services.Count != serviceIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more service IDs are invalid."
            });
        }

        provider.BusinessName = request.BusinessName.Trim();
        provider.Description = request.Description.Trim();

        provider.PhoneNumber = request.PhoneNumber.Trim();
        provider.WhatsAppNumber = request.WhatsAppNumber?.Trim();
        provider.Email = request.Email?.Trim();
        provider.Website = request.Website?.Trim();

        provider.AddressLine1 = request.AddressLine1.Trim();
        provider.AddressLine2 = request.AddressLine2?.Trim();
        provider.Suburb = request.Suburb.Trim();
        provider.City = request.City.Trim();
        provider.Province = request.Province.Trim();
        provider.PostalCode = request.PostalCode.Trim();

        provider.Latitude = request.Latitude;
        provider.Longitude = request.Longitude;

        provider.YearsInBusiness = request.YearsInBusiness;

        provider.IsVerified = request.IsVerified;
        provider.IsPublished = request.IsPublished;
        provider.IsActive = request.IsActive;

        provider.UpdatedAt = DateTime.UtcNow;

        _context.ProviderServices.RemoveRange(
            provider.ProviderServices);

        provider.ProviderServices = services
            .Select(service => new ProviderService
            {
                ProviderId = provider.Id,
                ServiceId = service.Id
            })
            .ToList();

        await _context.SaveChangesAsync();

        var updatedProvider = await _context.Providers
            .AsNoTracking()
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
                    .ThenInclude(s => s.ServiceCategory)
            .Include(p => p.BusinessHours)
            .FirstAsync(p => p.Id == id);

        return Ok(MapToResponse(updatedProvider));
    }

    // DELETE: api/providers/{id}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProvider(Guid id)
    {
        var provider = await _context.Providers
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        // Soft delete
        provider.IsActive = false;
        provider.IsPublished = false;
        provider.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ProviderResponse MapToResponse(
        Provider provider)
    {
        return new ProviderResponse
        {
            Id = provider.Id,

            BusinessName = provider.BusinessName,
            Description = provider.Description,

            PhoneNumber = provider.PhoneNumber,
            WhatsAppNumber = provider.WhatsAppNumber,
            Email = provider.Email,
            Website = provider.Website,

            AddressLine1 = provider.AddressLine1,
            AddressLine2 = provider.AddressLine2,
            Suburb = provider.Suburb,
            City = provider.City,
            Province = provider.Province,
            PostalCode = provider.PostalCode,

            Latitude = provider.Latitude,
            Longitude = provider.Longitude,

            YearsInBusiness = provider.YearsInBusiness,

            IsVerified = provider.IsVerified,
            IsPublished = provider.IsPublished,
            IsActive = provider.IsActive,

            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt,

            Services = provider.ProviderServices
                .Select(ps => new ServiceSummary
                {
                    Id = ps.Service.Id,
                    Name = ps.Service.Name,
                    CategoryName = ps.Service.ServiceCategory.Name
                })
                .ToList(),



                BusinessHours = provider.BusinessHours
                    .OrderBy(h => h.DayOfWeek)
                    .Select(h => new BusinessHourSummary
                    {
                        DayOfWeek = h.DayOfWeek,
                        OpenTime = h.OpenTime,
                        CloseTime = h.CloseTime,
                        IsClosed = h.IsClosed
                    })
                    .ToList()
        };
    }
}