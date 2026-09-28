using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FixLink.Domain.Entities;
using FixLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly FixLinkDbContext _context;

    public ReviewsController(FixLinkDbContext context)
    {
        _context = context;
    }

    // GET: api/reviews/provider/{providerId}
    // Public: returns approved reviews for a provider.
    [HttpGet("provider/{providerId:guid}")]
    public async Task<IActionResult> GetProviderReviews(
        Guid providerId)
    {
        var providerExists = await _context.Providers
            .AnyAsync(p =>
                p.Id == providerId &&
                p.IsActive &&
                p.IsPublished);

        if (!providerExists)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r =>
                r.ProviderId == providerId &&
                r.IsApproved &&
                !r.IsReported)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Title,
                r.Comment,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // POST: api/reviews/provider/{providerId}
    // Authenticated users can submit reviews.
    [Authorize]
    [HttpPost("provider/{providerId:guid}")]
    public async Task<IActionResult> CreateReview(
        Guid providerId,
        [FromBody] CreateReviewRequest request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify the logged-in user."
            });
        }

        var providerExists = await _context.Providers
            .AnyAsync(p =>
                p.Id == providerId &&
                p.IsActive &&
                p.IsPublished);

        if (!providerExists)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            UserId = userId,
            Rating = request.Rating,
            Title = request.Title,
            Comment = request.Comment.Trim(),
            IsApproved = false,
            IsReported = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        var response = new
        {
            review.Id,
            review.ProviderId,
            review.Rating,
            review.Title,
            review.Comment,
            review.IsApproved,
            review.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetProviderReviews),
            new { providerId },
            response);
    }

    public class CreateReviewRequest
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 3)]
        public string Comment { get; set; } = string.Empty;
    }



    // GET: api/reviews/pending
    // Admin only: returns reviews awaiting approval.
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingReviews()
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => !r.IsApproved && !r.IsReported)
            .OrderBy(r => r.CreatedAt)
            .Join(
                _context.Providers,
                review => review.ProviderId,
                provider => provider.Id,
                (review, provider) => new
                {
                    review.Id,
                    review.ProviderId,
                    ProviderName = provider.BusinessName,
                    review.UserId,
                    review.Rating,
                    review.Title,
                    review.Comment,
                    review.CreatedAt,
                    review.IsApproved,
                    review.IsReported
                })
            .ToListAsync();

        return Ok(reviews);
    }



    // PUT: api/reviews/{id}/approve
    // Admin only: approves a review.
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> ApproveReview(Guid id)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            return NotFound(new
            {
                message = "Review not found."
            });
        }

        if (review.IsReported)
        {
            return BadRequest(new
            {
                message = "A reported review cannot be approved."
            });
        }

        review.IsApproved = true;
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Review approved successfully.",
            review.Id,
            review.IsApproved,
            review.UpdatedAt
        });
    }






}