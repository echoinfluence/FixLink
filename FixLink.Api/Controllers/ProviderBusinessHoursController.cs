using FixLink.Domain.Entities;
using FixLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/providers/{providerId:guid}/business-hours")]
[Authorize(Roles = "Admin")]
public class ProviderBusinessHoursController : ControllerBase
{
    private readonly FixLinkDbContext _context;

    public ProviderBusinessHoursController(
        FixLinkDbContext context)
    {
        _context = context;
    }

    // GET: api/providers/{providerId}/business-hours
    [HttpGet]
    public async Task<IActionResult> GetBusinessHours(Guid providerId)
    {
        var providerExists = await _context.Providers
            .AnyAsync(p => p.Id == providerId);

        if (!providerExists)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var savedHours = await _context.ProviderBusinessHours
            .AsNoTracking()
            .Where(h => h.ProviderId == providerId)
            .ToListAsync();

        var response = Enum.GetValues<DayOfWeek>()
            .OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d)
            .Select(day =>
            {
                var hours = savedHours
                    .FirstOrDefault(h => h.DayOfWeek == day);

                return new BusinessHourResponse
                {
                    DayOfWeek = day,
                    IsConfigured = hours != null,
                    OpenTime = hours?.OpenTime,
                    CloseTime = hours?.CloseTime,
                    IsClosed = hours?.IsClosed ?? false
                };
            })
            .ToList();

        return Ok(response);
    }

    // PUT: api/providers/{providerId}/business-hours
    [HttpPut]
    public async Task<IActionResult> UpdateBusinessHours(
        Guid providerId,
        [FromBody] UpdateBusinessHoursRequest request)
    {
        var providerExists = await _context.Providers
            .AnyAsync(p => p.Id == providerId);

        if (!providerExists)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        if (request.Days == null ||
            request.Days.Count != 7)
        {
            return BadRequest(new
            {
                message = "Exactly seven days must be supplied."
            });
        }

        if (request.Days.Any(d =>
                !Enum.IsDefined(typeof(DayOfWeek), d.DayOfWeek)) ||
            request.Days.Select(d => d.DayOfWeek)
                .Distinct()
                .Count() != 7)
        {
            return BadRequest(new
            {
                message = "Each day of the week must appear exactly once."
            });
        }

        foreach (var day in request.Days)
        {
            if (day.IsClosed)
            {
                continue;
            }

            if (!day.OpenTime.HasValue ||
                !day.CloseTime.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        $"Opening and closing times are required for {day.DayOfWeek}."
                });
            }

            if (day.CloseTime.Value <= day.OpenTime.Value)
            {
                return BadRequest(new
                {
                    message =
                        $"Closing time must be after opening time for {day.DayOfWeek}."
                });
            }
        }

        var existingHours = await _context.ProviderBusinessHours
            .Where(h => h.ProviderId == providerId)
            .ToListAsync();

        foreach (var day in request.Days)
        {
            var hours = existingHours
                .FirstOrDefault(h => h.DayOfWeek == day.DayOfWeek);

            if (hours == null)
            {
                hours = new ProviderBusinessHour
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    DayOfWeek = day.DayOfWeek
                };

                _context.ProviderBusinessHours.Add(hours);
            }

            hours.IsClosed = day.IsClosed;

            hours.OpenTime = day.IsClosed
                ? null
                : day.OpenTime;

            hours.CloseTime = day.IsClosed
                ? null
                : day.CloseTime;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Business hours updated successfully."
        });
    }
}

public class UpdateBusinessHoursRequest
{
    public List<BusinessHourInput> Days { get; set; } = new();
}

public class BusinessHourInput
{
    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan? OpenTime { get; set; }

    public TimeSpan? CloseTime { get; set; }

    public bool IsClosed { get; set; }
}

public class BusinessHourResponse
{
    public DayOfWeek DayOfWeek { get; set; }

    public bool IsConfigured { get; set; }

    public TimeSpan? OpenTime { get; set; }

    public TimeSpan? CloseTime { get; set; }

    public bool IsClosed { get; set; }
}