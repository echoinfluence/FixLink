using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class SavedProvider
{
    public Guid UserId { get; set; }

    public Guid ProviderId { get; set; }

    public DateTime SavedAt { get; set; }

    public Provider Provider { get; set; } = null!;
}