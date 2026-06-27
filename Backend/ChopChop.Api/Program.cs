using System.Text;
using ChopChop.Api.Data;
using ChopChop.Api.Domain;
using ChopChop.Api.Endpoints;
using ChopChop.Api.Realtime;
using ChopChop.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Configuration
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
builder.Services.Configure<JwtOptions>(jwtSection);

// ---------------------------------------------------------------- Database
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=chopchop.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// ---------------------------------------------------------------- Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------------------------------------------------------------- AuthN / AuthZ
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(15),
        };

        // WebSockets can't send an Authorization header, so SignalR passes the JWT as ?access_token=.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------- App services
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<IFarmService, FarmService>();
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSingleton<ISeedCatalog, SeedCatalog>();

builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p
        .SetIsOriginAllowed(_ => true) // dev: allow any origin while supporting credentials for SignalR
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

// ---------------------------------------------------------------- Migrate + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// ---------------------------------------------------------------- Pipeline
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "ChopChop.Api", status = "online" }));
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapLeaderboardEndpoints();
app.MapInventoryEndpoints();
app.MapRoomEndpoints();
app.MapTradeEndpoints();
app.MapFarmEndpoints();

app.MapHub<GameHub>("/hub/game");

app.Run();
