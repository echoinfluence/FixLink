using FixLink.Infrastructure.Data;
using FixLink.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using FixLink.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


// Local provider image storage
var webRootPath = builder.Environment.WebRootPath
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

var providerImagesPath = Path.Combine(
    webRootPath,
    "uploads",
    "providers");

Directory.CreateDirectory(providerImagesPath);

builder.Services.AddSingleton<IFileStorageService>(
    _ => new LocalFileStorageService(providerImagesPath));


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddDbContext<FixLinkDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


//builder.Services.AddSingleton<TimeProvider>(
//    TimeProvider.System);
builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddEntityFrameworkStores<FixLinkDbContext>();
//.AddDefaultTokenProviders();

///////////////////
//var testProvider = builder.Services
//    .BuildServiceProvider()
//    .GetRequiredService<TimeProvider>();
//////////////
///



builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtKey =
            builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is not configured.");

        var jwtIssuer =
            builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var jwtAudience =
            builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();





var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var dbContext =
        services.GetRequiredService<FixLinkDbContext>();

    await IdentitySeeder.SeedAsync(
        userManager,
        roleManager);

    await ServiceSeeder.SeedAsync(
        dbContext);
}




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
