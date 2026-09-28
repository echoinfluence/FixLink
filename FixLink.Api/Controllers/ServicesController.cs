using FixLink.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly FixLinkDbContext _context;

    public ServicesController(FixLinkDbContext context)
    {
        _context = context;
    }

    // GET: api/services
    // GET: api/services?categoryId={id}
    [HttpGet]
    public async Task<IActionResult> GetServices(
        [FromQuery] Guid? categoryId)
    {
        var query = _context.ServiceCategories
            .AsNoTracking()
            .SelectMany(category => category.Services
                .Where(service => service.IsActive)
                .Select(service => new
                {
                    service.Id,
                    service.Name,
                    service.Description,
                    CategoryId = category.Id,
                    CategoryName = category.Name
                }));

        if (categoryId.HasValue)
        {
            query = query.Where(
                service => service.CategoryId == categoryId.Value);
        }

        var services = await query
            .OrderBy(service => service.CategoryName)
            .ThenBy(service => service.Name)
            .ToListAsync();

        return Ok(services);
    }

    // GET: api/services/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetService(Guid id)
    {
        var service = await _context.ServiceCategories
            .AsNoTracking()
            .SelectMany(category => category.Services
                .Where(service =>
                    service.Id == id && service.IsActive)
                .Select(service => new
                {
                    service.Id,
                    service.Name,
                    service.Description,
                    CategoryId = category.Id,
                    CategoryName = category.Name
                }))
            .FirstOrDefaultAsync();

        if (service == null)
        {
            return NotFound(new
            {
                message = "Service not found."
            });
        }

        return Ok(service);
    }
}