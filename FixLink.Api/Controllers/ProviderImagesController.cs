
using FixLink.Domain.Entities;
using FixLink.Infrastructure.Data;
using FixLink.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Api.Controllers;

[ApiController]
[Route("api/providers/{providerId:guid}/images")]
public class ProviderImagesController : ControllerBase
{
    private readonly FixLinkDbContext _context;
    private readonly IFileStorageService _fileStorage;

    private const long MaxFileSize = 5 * 1024 * 1024;

    public ProviderImagesController(
        FixLinkDbContext context,
        IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }






    // GET: api/providers/{providerId}/images
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetProviderImages(
        Guid providerId,
        CancellationToken cancellationToken)
    {
        var provider = await _context.Providers
            .FirstOrDefaultAsync(
                p => p.Id == providerId &&
                     p.IsActive &&
                     p.IsPublished,
                cancellationToken);

        if (provider == null)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var images = await _context.ProviderImages
            .Where(image => image.ProviderId == providerId)
            .OrderBy(image => image.DisplayOrder)
            .Select(image => new
            {
                image.Id,
                image.ProviderId,
                image.BlobName,
                image.BlobUrl,
                image.Caption,
                image.DisplayOrder,
                image.IsPrimary,
                image.UploadedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(images);
    }





    // PUT: api/providers/{providerId}/images/{imageId}/primary
    [Authorize(Roles = "Admin")]
    [HttpPut("{imageId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryImage(
        Guid providerId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var images = await _context.ProviderImages
            .Where(image => image.ProviderId == providerId)
            .ToListAsync(cancellationToken);

        if (images.Count == 0)
        {
            var providerExists = await _context.Providers
                .AnyAsync(
                    provider => provider.Id == providerId,
                    cancellationToken);

            if (!providerExists)
            {
                return NotFound(new
                {
                    message = "Provider not found."
                });
            }

            return NotFound(new
            {
                message = "This provider has no images."
            });
        }

        var selectedImage = images
            .FirstOrDefault(image => image.Id == imageId);

        if (selectedImage == null)
        {
            return NotFound(new
            {
                message = "Image not found for this provider."
            });
        }

        foreach (var image in images)
        {
            image.IsPrimary = image.Id == imageId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Primary image updated successfully.",
            imageId = selectedImage.Id,
            selectedImage.IsPrimary
        });
    }






    // DELETE: api/providers/{providerId}/images/{imageId}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(
        Guid providerId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var image = await _context.ProviderImages
            .FirstOrDefaultAsync(
                item => item.Id == imageId &&
                        item.ProviderId == providerId,
                cancellationToken);

        if (image == null)
        {
            return NotFound(new
            {
                message = "Image not found for this provider."
            });
        }

        var wasPrimary = image.IsPrimary;
        var fileName = image.BlobName;

        _context.ProviderImages.Remove(image);

        await _context.SaveChangesAsync(cancellationToken);

        await _fileStorage.DeleteAsync(
            fileName,
            cancellationToken);

        // If the primary image was deleted, promote another image.
        if (wasPrimary)
        {
            var nextImage = await _context.ProviderImages
                .Where(item => item.ProviderId == providerId)
                .OrderBy(item => item.DisplayOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextImage != null)
            {
                nextImage.IsPrimary = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return Ok(new
        {
            message = "Image deleted successfully."
        });
    }







    // GET: api/providers/{providerId}/images/admin
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetProviderImagesAdmin(
        Guid providerId,
        CancellationToken cancellationToken)
    {
        var providerExists = await _context.Providers
            .AnyAsync(
                provider => provider.Id == providerId,
                cancellationToken);

        if (!providerExists)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        var images = await _context.ProviderImages
            .Where(image => image.ProviderId == providerId)
            .OrderBy(image => image.DisplayOrder)
            .Select(image => new
            {
                image.Id,
                image.ProviderId,
                image.BlobName,
                image.BlobUrl,
                image.Caption,
                image.DisplayOrder,
                image.IsPrimary,
                image.UploadedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(images);
    }






    // POST: api/providers/{providerId}/images
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadImage(
        Guid providerId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select an image to upload."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "The image must be 5 MB or smaller."
            });
        }

        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new
            {
                message = "Only JPEG, PNG and WebP images are allowed."
            });
        }

        var provider = await _context.Providers
            .FirstOrDefaultAsync(
                p => p.Id == providerId,
                cancellationToken);

        if (provider == null)
        {
            return NotFound(new
            {
                message = "Provider not found."
            });
        }

        // Check the actual file signature, not just its MIME type.
        await using var inputStream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await inputStream.ReadAsync(
            header.AsMemory(0, header.Length),
            cancellationToken);

        inputStream.Position = 0;

        var contentType = file.ContentType.ToLowerInvariant();

        var isValidImage = contentType switch
        {
            "image/jpeg" =>
                bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF,

            "image/png" =>
                bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A,

            "image/webp" =>
                bytesRead >= 12 &&
                header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'E' &&
                header[10] == (byte)'B' &&
                header[11] == (byte)'P',

            _ => false
        };

        if (!isValidImage)
        {
            return BadRequest(new
            {
                message = "The uploaded file is not a valid supported image."
            });
        }

        string imageUrl = string.Empty;

        try
        {
            imageUrl = await _fileStorage.SaveAsync(
                inputStream,
                file.FileName,
                contentType,
                cancellationToken);

            var existingImageCount = await _context.ProviderImages
                .CountAsync(
                    image => image.ProviderId == providerId,
                    cancellationToken);

            var image = new ProviderImage
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                BlobName = Path.GetFileName(imageUrl),
                BlobUrl = imageUrl,
                Caption = null,
                DisplayOrder = existingImageCount,
                IsPrimary = existingImageCount == 0,
                UploadedAt = DateTime.UtcNow
            };

            _context.ProviderImages.Add(image);

            await _context.SaveChangesAsync(cancellationToken);

            return Created(imageUrl, new
            {
                image.Id,
                image.ProviderId,
                image.BlobName,
                image.BlobUrl,
                image.Caption,
                image.DisplayOrder,
                image.IsPrimary,
                image.UploadedAt
            });
        }
        catch
        {
            // If saving the database record fails, remove the file.
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                await _fileStorage.DeleteAsync(
                    Path.GetFileName(imageUrl),
                    CancellationToken.None);
            }

            throw;
        }
    }
}
