using System.Reflection.Metadata;
using System.Text;
using FinalcapstoneDriversLicense.Data;
using FinalcapstoneDriversLicense.Entities;
using FinalcapstoneDriversLicense.Service.IService;
using FinalcapstoneDriversLicense.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        });
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
// Example using AddIdentity:


builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(x =>
{
    //Password Settings
    x.Password.RequiredLength = 6;
    x.Password.RequireDigit = true;
    x.Password.RequireLowercase = true;
    x.Password.RequireUppercase = true;
    x.Password.RequireNonAlphanumeric = true;
})
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

var app = builder.Build();


//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

///Seed roles and a development staff account. Change the password before deployment.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Replace with your actual DbContext class name
    dbContext.Database.Migrate(); // Or dbContext.Database.EnsureCreated();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var role in new[] { "Applicant", "Staff", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    var staffEmail = builder.Configuration["SeedStaff:Email"];
    var staffPassword = builder.Configuration["SeedStaff:Password"];
    if (!string.IsNullOrWhiteSpace(staffEmail) && !string.IsNullOrWhiteSpace(staffPassword))
    {
        var staff = await userManager.FindByEmailAsync(staffEmail);
        if (staff is null)
        {
            staff = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                FullName = "Stephen Staff",
                PhoneNumber = "0700000000",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(staff, staffPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(staff, "Staff");
            else
                app.Logger.LogWarning("Development staff seed failed: {Errors}",
                 string.Join("; ", result.Errors.Select(e => e.Description)));
        }

    }
    var adminEmail = builder.Configuration["SeedAdmin:Email"];
    var adminPassword = builder.Configuration["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Thuri Kazora",
                PhoneNumber = "0700000000",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                app.Logger.LogWarning("Development admin seed failed: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
  app.Run();
