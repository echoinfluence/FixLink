using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class ProviderReport
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public Guid UserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Provider Provider { get; set; } = null!;
}
