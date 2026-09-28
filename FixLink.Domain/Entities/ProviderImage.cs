using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class ProviderImage
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string BlobName { get; set; } = string.Empty;

    public string BlobUrl { get; set; } = string.Empty;

    public string? Caption { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime UploadedAt { get; set; }

    public Provider Provider { get; set; } = null!;
}
