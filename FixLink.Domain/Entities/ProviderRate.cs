using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class ProviderRate
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public Guid ServiceId { get; set; }

    public decimal Price { get; set; }

    public string PriceType { get; set; } = "Starting From";

    public string? Description { get; set; }

    public Provider Provider { get; set; } = null!;

    public Service Service { get; set; } = null!;
}