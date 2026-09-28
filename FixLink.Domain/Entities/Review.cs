using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public Guid UserId { get; set; }

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string Comment { get; set; } = string.Empty;

    public bool IsApproved { get; set; }

    public bool IsReported { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Provider Provider { get; set; } = null!;
}