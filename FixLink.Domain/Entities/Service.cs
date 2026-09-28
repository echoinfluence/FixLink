using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class Service
{
    public Guid Id { get; set; }

    public Guid ServiceCategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ServiceCategory ServiceCategory { get; set; } = null!;

    public ICollection<ProviderService> ProviderServices { get; set; }
        = new List<ProviderService>();

    public ICollection<ProviderRate> ProviderRates { get; set; }
        = new List<ProviderRate>();
}
