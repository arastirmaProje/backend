using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Personelim.Data;
using Personelim.Services.Auth;
using Personelim.Services.Invitation;
using Personelim.Services.Location;
using Personelim.Validators;
using Personelim.Services;
using Personelim.Services.Business;
using Personelim.Services.BusinessMember;
using Personelim.Services.Email;
using Personelim.Services.Leave;
using Personelim.Services.Task;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Personelim.Services.Shift;
using Personelim.Services.Admin;
using Personelim.Services.Department;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// CONFIGURATION (JSON + ENV)  ✅ Render uyumlu
// =======================================================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// =======================================================
// DATABASE
// =======================================================
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Database connection string not found");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// =======================================================
// JWT CONFIG
// =======================================================
var jwtKey =
    Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new Exception("JWT_KEY missing");

var jwtIssuer =
    Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"];

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtKey)
);

// =======================================================
// EMAIL CONFIG
// =======================================================
builder.Services.Configure<SmtpSettings>(options =>
{
    options.Host     = Environment.GetEnvironmentVariable("SMTP_HOST")     ?? builder.Configuration["Smtp:Host"]     ?? "smtp.gmail.com";
    options.Port     = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? builder.Configuration["Smtp:Port"], out var port) ? port : 587;
    options.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? builder.Configuration["Smtp:Username"] ?? throw new Exception("SMTP_USERNAME missing");
    options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? builder.Configuration["Smtp:Password"] ?? throw new Exception("SMTP_PASSWORD missing");
    options.FromEmail= Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL")  ?? builder.Configuration["Smtp:FromEmail"]  ?? throw new Exception("SMTP_FROM_EMAIL missing");
    options.FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")   ?? builder.Configuration["Smtp:FromName"]   ?? "Personelim App";
});

// =======================================================
// LOCALIZATION
// =======================================================
builder.Services.AddLocalization();

// =======================================================
// SERVICES
// =======================================================
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBusinessValidator, BusinessValidator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IBusinessMemberService, BusinessMemberService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<Personelim.Services.Performance.IPerformanceService, Personelim.Services.Performance.PerformanceService>();

builder.Services.AddHttpClient("AiPerformance", c =>
{
    c.BaseAddress = new Uri("https://personelim-ai-api.onrender.com/api/performans");
    c.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// =======================================================
// CORS
// =======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// =======================================================
// SWAGGER
// =======================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Personelim API",
        Version = "v1",
        Description = "Personelim personel yönetim sistemi API dokümantasyonu. " +
                      "Tüm istekler için Authorization header'ına 'Bearer {token}' formatında JWT token gönderilmelidir."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =======================================================
// BUILD APP
// =======================================================
var app = builder.Build();

// =======================================================
// DATABASE MIGRATION (SAFE)
// =======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// =======================================================
// HTTP PIPELINE
// =======================================================
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
// =========================================================================
// 🔥 GARANTİLİ DOSYA SUNUCUSU AYARI 🔥
// =========================================================================

// 1. Projenin çalıştığı klasörü bul ve "wwwroot" yolunu oluştur
string currentDirectory = Directory.GetCurrentDirectory();
string wwwrootPath = Path.Combine(currentDirectory, "wwwroot");

// 2. Klasör fiziksel olarak yoksa oluştur (Hata almamak için)
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

// 3. Bu klasörü dış dünyaya aç
app.UseStaticFiles(new StaticFileOptions
{
    // Dosyaları kesinlikle bu klasörden oku
    FileProvider = new PhysicalFileProvider(wwwrootPath),
    
    // URL'de bir ön ek istemiyoruz. 
    // Yani: localhost:5059/uploads/resim.jpg diyeceğiz.
    RequestPath = "" 
});
// =========================================================================
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();