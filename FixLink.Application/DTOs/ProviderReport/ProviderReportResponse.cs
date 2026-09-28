using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Application.DTOs.ProviderReport;

public class ProviderReportResponse
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}