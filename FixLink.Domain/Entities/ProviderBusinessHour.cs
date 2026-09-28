using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class ProviderBusinessHour
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan? OpenTime { get; set; }

    public TimeSpan? CloseTime { get; set; }

    public bool IsClosed { get; set; }

    public Provider Provider { get; set; } = null!;
}