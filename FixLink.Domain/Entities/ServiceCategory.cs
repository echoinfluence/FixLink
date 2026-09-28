using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class ServiceCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public ICollection<Service> Services { get; set; }
        = new List<Service>();
}
