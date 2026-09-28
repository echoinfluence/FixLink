using System.Security.Claims;
using FixLink.Application.DTOs.ProviderReport;
using FixLink.Domain.Entities;
using FixLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/providerreports")]
public class ProviderReportsController : ControllerBase
{
    private readonly FixLinkDbContext _context;

    public ProviderReportsController(FixLinkDbContext context)
    {
        _context = context;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.Parse(userId!);
    }

    // POST: api/providerreports/{providerId}
    [HttpPost("{providerId:guid}")]
    [Authorize]
    public async Task<IActionResult> CreateReport(
        Guid providerId,
        [FromBody] CreateProviderReportRequest request)
    {
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

        var report = new ProviderReport
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            UserId = GetUserId(),
            Reason = request.Reason.Trim(),
            Description = request.Description?.Trim(),
            IsResolved = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProviderReports.Add(report);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetMyReports),
            new
            {
                id = report.Id
            },
            new
            {
                message = "Provider reported successfully.",
                report.Id,
                report.ProviderId,
                report.Reason,
                report.IsResolved,
                report.CreatedAt
            });
    }

    // GET: api/providerreports/my-reports
    [HttpGet("my-reports")]
    [Authorize]
    public async Task<ActionResult<List<ProviderReportResponse>>>
        GetMyReports()
    {
        var userId = GetUserId();

        var reports = await _context.ProviderReports
            .AsNoTracking()
            .Include(r => r.Provider)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProviderReportResponse
            {
                Id = r.Id,
                ProviderId = r.ProviderId,
                ProviderName = r.Provider.BusinessName,
                UserId = r.UserId,
                Reason = r.Reason,
                Description = r.Description,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            })
            .ToListAsync();

        return Ok(reports);
    }

    // GET: api/providerreports
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ProviderReportResponse>>>
        GetAllReports()
    {
        var reports = await _context.ProviderReports
            .AsNoTracking()
            .Include(r => r.Provider)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProviderReportResponse
            {
                Id = r.Id,
                ProviderId = r.ProviderId,
                ProviderName = r.Provider.BusinessName,
                UserId = r.UserId,
                Reason = r.Reason,
                Description = r.Description,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            })
            .ToListAsync();

        return Ok(reports);
    }

    // PUT: api/providerreports/{id}/resolve
    [HttpPut("{id:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResolveReport(Guid id)
    {
        var report = await _context.ProviderReports
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
        {
            return NotFound(new
            {
                message = "Report not found."
            });
        }

        if (report.IsResolved)
        {
            return Ok(new
            {
                message = "Report has already been resolved.",
                report.Id,
                report.IsResolved,
                report.ResolvedAt
            });
        }

        report.IsResolved = true;
        report.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Report resolved successfully.",
            report.Id,
            report.IsResolved,
            report.ResolvedAt
        });
    }
}