using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class ProviderService
{
    public Guid ProviderId { get; set; }

    public Guid ServiceId { get; set; }

    public Provider Provider { get; set; } = null!;

    public Service Service { get; set; } = null!;
}
