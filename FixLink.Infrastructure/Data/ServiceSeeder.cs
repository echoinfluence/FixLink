using FixLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Infrastructure.Data;

public static class ServiceSeeder
{
    public static async Task SeedAsync(FixLinkDbContext context)
    {
        var categories = new[]
        {
            new
            {
                Name = "Plumbing",
                Description = "Plumbing installation, repairs and maintenance.",
                Icon = "🔧",
                DisplayOrder = 1,
                Services = new[]
                {
                    "General Plumbing",
                    "Leak Repairs",
                    "Drain Cleaning",
                    "Geyser Installation",
                    "Geyser Repairs"
                }
            },
            new
            {
                Name = "Electrical",
                Description = "Electrical installation, repairs and maintenance.",
                Icon = "⚡",
                DisplayOrder = 2,
                Services = new[]
                {
                    "Electrical Repairs",
                    "Electrical Wiring",
                    "Lighting Installation",
                    "DB Board Installation",
                    "Electrical Compliance Certificates"
                }
            },
            new
            {
                Name = "Building & Construction",
                Description = "Building, renovations and construction services.",
                Icon = "🏗️",
                DisplayOrder = 3,
                Services = new[]
                {
                    "General Building",
                    "Home Renovations",
                    "Brickwork",
                    "Plastering",
                    "Tiling"
                }
            },
            new
            {
                Name = "Painting",
                Description = "Interior and exterior painting services.",
                Icon = "🎨",
                DisplayOrder = 4,
                Services = new[]
                {
                    "Interior Painting",
                    "Exterior Painting",
                    "Residential Painting",
                    "Commercial Painting",
                    "Roof Painting"
                }
            },
            new
            {
                Name = "Cleaning",
                Description = "Residential and commercial cleaning services.",
                Icon = "🧹",
                DisplayOrder = 5,
                Services = new[]
                {
                    "House Cleaning",
                    "Office Cleaning",
                    "Deep Cleaning",
                    "Carpet Cleaning",
                    "Window Cleaning"
                }
            },
            new
            {
                Name = "Gardening & Landscaping",
                Description = "Garden maintenance and landscaping services.",
                Icon = "🌿",
                DisplayOrder = 6,
                Services = new[]
                {
                    "Garden Maintenance",
                    "Lawn Care",
                    "Landscaping",
                    "Tree Trimming",
                    "Irrigation Installation"
                }
            },
            new
            {
                Name = "Automotive",
                Description = "Vehicle repairs, maintenance and automotive services.",
                Icon = "🚗",
                DisplayOrder = 7,
                Services = new[]
                {
                    "Vehicle Servicing",
                    "Brake Repairs",
                    "Battery Replacement",
                    "Auto Electrical",
                    "Vehicle Diagnostics"
                }
            },
            new
            {
                Name = "Air Conditioning",
                Description = "Air conditioning installation, repairs and maintenance.",
                Icon = "❄️",
                DisplayOrder = 8,
                Services = new[]
                {
                    "Aircon Installation",
                    "Aircon Repairs",
                    "Aircon Servicing",
                    "Aircon Cleaning",
                    "Commercial Air Conditioning"
                }
            },
            new
            {
                Name = "Appliance Repair",
                Description = "Repair and maintenance of household appliances.",
                Icon = "🔌",
                DisplayOrder = 9,
                Services = new[]
                {
                    "Washing Machine Repair",
                    "Fridge Repair",
                    "Dishwasher Repair",
                    "Oven Repair",
                    "Stove Repair"
                }
            },
            new
            {
                Name = "Security",
                Description = "Security installation and related services.",
                Icon = "🔒",
                DisplayOrder = 10,
                Services = new[]
                {
                    "CCTV Installation",
                    "Alarm Installation",
                    "Electric Fencing",
                    "Access Control",
                    "Security System Repairs"
                }
            },
            new
            {
                Name = "Pest Control",
                Description = "Residential and commercial pest control services.",
                Icon = "🐜",
                DisplayOrder = 11,
                Services = new[]
                {
                    "General Pest Control",
                    "Cockroach Control",
                    "Rodent Control",
                    "Termite Control",
                    "Wasp Removal"
                }
            },
            new
            {
                Name = "Moving & Removals",
                Description = "Household, office and furniture moving services.",
                Icon = "🚚",
                DisplayOrder = 12,
                Services = new[]
                {
                    "House Removals",
                    "Office Removals",
                    "Furniture Moving",
                    "Packing Services",
                    "Long Distance Removals"
                }
            }
        };

        foreach (var categoryData in categories)
        {
            var category =
                await context.ServiceCategories
                    .FirstOrDefaultAsync(c =>
                        c.Name == categoryData.Name);

            if (category == null)
            {
                category = new ServiceCategory
                {
                    Id = Guid.NewGuid(),
                    Name = categoryData.Name,
                    Description = categoryData.Description,
                    Icon = categoryData.Icon,
                    IsActive = true,
                    DisplayOrder = categoryData.DisplayOrder
                };

                context.ServiceCategories.Add(category);

                await context.SaveChangesAsync();
            }

            foreach (var serviceName in categoryData.Services)
            {
                var serviceExists =
                    await context.Services.AnyAsync(s =>
                        s.Name == serviceName &&
                        s.ServiceCategoryId == category.Id);

                if (!serviceExists)
                {
                    context.Services.Add(new Service
                    {
                        Id = Guid.NewGuid(),
                        ServiceCategoryId = category.Id,
                        Name = serviceName,
                        IsActive = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}