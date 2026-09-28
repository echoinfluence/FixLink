using FixLink.Domain.Entities;
using FixLink.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FixLink.Infrastructure.Data;

public class FixLinkDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public FixLinkDbContext(
        DbContextOptions<FixLinkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();

    public DbSet<ProviderImage> ProviderImages => Set<ProviderImage>();

    public DbSet<ProviderRate> ProviderRates => Set<ProviderRate>();

    public DbSet<ProviderBusinessHour> ProviderBusinessHours => Set<ProviderBusinessHour>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<SavedProvider> SavedProviders => Set<SavedProvider>();

    public DbSet<ProviderReport> ProviderReports => Set<ProviderReport>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureProvider(builder);
        ConfigureServiceCategory(builder);
        ConfigureService(builder);
        ConfigureProviderService(builder);
        ConfigureProviderImage(builder);
        ConfigureProviderRate(builder);
        ConfigureProviderBusinessHour(builder);
        ConfigureReview(builder);
        ConfigureSavedProvider(builder);
        ConfigureProviderReport(builder);
        ConfigureAuditLog(builder);
    }

    private static void ConfigureProvider(ModelBuilder builder)
    {
        builder.Entity<Provider>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.BusinessName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .HasMaxLength(4000);

            entity.Property(p => p.PhoneNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(p => p.WhatsAppNumber)
                .HasMaxLength(30);

            entity.Property(p => p.Email)
                .HasMaxLength(320);

            entity.Property(p => p.Website)
                .HasMaxLength(500);

            entity.Property(p => p.AddressLine1)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(p => p.AddressLine2)
                .HasMaxLength(250);

            entity.Property(p => p.Suburb)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.City)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Province)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.PostalCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .IsRequired();

            entity.HasIndex(p => new
            {
                p.City,
                p.Province,
                p.IsPublished,
                p.IsActive
            });

            entity.HasIndex(p => p.BusinessName);
        });
    }

    private static void ConfigureServiceCategory(ModelBuilder builder)
    {
        builder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.Icon)
                .HasMaxLength(100);

            entity.HasIndex(c => c.Name)
                .IsUnique();
        });
    }

    private static void ConfigureService(ModelBuilder builder)
    {
        builder.Entity<Service>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(s => s.Description)
                .HasMaxLength(500);

            entity.HasOne(s => s.ServiceCategory)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.Name);
        });
    }

    private static void ConfigureProviderService(ModelBuilder builder)
    {
        builder.Entity<ProviderService>(entity =>
        {
            entity.HasKey(ps => new
            {
                ps.ProviderId,
                ps.ServiceId
            });

            entity.HasOne(ps => ps.Provider)
                .WithMany(p => p.ProviderServices)
                .HasForeignKey(ps => ps.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.Service)
                .WithMany(s => s.ProviderServices)
                .HasForeignKey(ps => ps.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProviderImage(ModelBuilder builder)
    {
        builder.Entity<ProviderImage>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.BlobName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(i => i.BlobUrl)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(i => i.Caption)
                .HasMaxLength(250);

            entity.HasOne(i => i.Provider)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => new
            {
                i.ProviderId,
                i.DisplayOrder
            });
        });
    }

    private static void ConfigureProviderRate(ModelBuilder builder)
    {
        builder.Entity<ProviderRate>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Price)
                .HasPrecision(18, 2);

            entity.Property(r => r.PriceType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(r => r.Description)
                .HasMaxLength(500);

            entity.HasOne(r => r.Provider)
                .WithMany(p => p.Rates)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Service)
                .WithMany(s => s.ProviderRates)
                .HasForeignKey(r => r.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProviderBusinessHour(ModelBuilder builder)
    {
        builder.Entity<ProviderBusinessHour>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.HasOne(h => h.Provider)
                .WithMany(p => p.BusinessHours)
                .HasForeignKey(h => h.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(h => new
            {
                h.ProviderId,
                h.DayOfWeek
            })
            .IsUnique();
        });
    }

    private static void ConfigureReview(ModelBuilder builder)
    {
        builder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Rating)
                .IsRequired();

            entity.Property(r => r.Title)
                .HasMaxLength(150);

            entity.Property(r => r.Comment)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasOne(r => r.Provider)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new
            {
                r.ProviderId,
                r.CreatedAt
            });

            entity.HasIndex(r => new
            {
                r.ProviderId,
                r.UserId
            });
        });
    }

    private static void ConfigureSavedProvider(ModelBuilder builder)
    {
        builder.Entity<SavedProvider>(entity =>
        {
            entity.HasKey(sp => new
            {
                sp.UserId,
                sp.ProviderId
            });

            entity.HasOne(sp => sp.Provider)
                .WithMany(p => p.SavedByUsers)
                .HasForeignKey(sp => sp.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(sp => sp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProviderReport(ModelBuilder builder)
    {
        builder.Entity<ProviderReport>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Reason)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(r => r.Description)
                .HasMaxLength(2000);

            entity.HasOne(r => r.Provider)
                .WithMany()
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.EntityId)
                .HasMaxLength(100);

            entity.Property(a => a.Description)
                .HasMaxLength(2000);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(a => a.CreatedAt);

            entity.HasIndex(a => new
            {
                a.EntityType,
                a.EntityId
            });
        });
    }
}