using System;
using System.Collections.Generic;
using System.Text;

namespace FixLink.Domain.Entities;

public class Provider
{
    public Guid Id { get; set; }

    // Business Information
    public string BusinessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Contact Information
    public string PhoneNumber { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Location
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Business Information
    public int? YearsInBusiness { get; set; }

    // Directory Status
    public bool IsVerified { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<ProviderService> ProviderServices { get; set; }
        = new List<ProviderService>();

    public ICollection<ProviderImage> Images { get; set; }
        = new List<ProviderImage>();

    public ICollection<ProviderRate> Rates { get; set; }
        = new List<ProviderRate>();

    public ICollection<ProviderBusinessHour> BusinessHours { get; set; }
        = new List<ProviderBusinessHour>();

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();

    public ICollection<SavedProvider> SavedByUsers { get; set; }
        = new List<SavedProvider>();
}
