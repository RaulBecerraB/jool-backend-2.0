using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using jool_backend.Repository;
using jool_backend.Services;
using jool_backend.Validations;
using FluentValidation;
using FluentValidation.AspNetCore;
using jool_backend.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using DotNetEnv;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;

// Cargar variables de entorno desde el archivo .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<CreateHashtagValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Jool API", 
        Version = "v1",
        Description = "API para la plataforma Jool"
    });
    
    // Configurar seguridad JWT para Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
    
    // Configuración para manejar rutas duplicadas
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// Configuración para deshabilitar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Configuración de JWT
var jwtSection = builder.Configuration.GetSection("JWT");
var secretKey = jwtSection["SecretKey"];
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// Configuración para autenticación JWT solamente (sin Microsoft)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Configurar eventos para manejar errores
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { message = "El token ha expirado" });
                return context.Response.WriteAsync(result);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { message = "Token inválido" });
                return context.Response.WriteAsync(result);
            }
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { message = "No estás autorizado. Se requiere un token válido." });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { message = "No tienes permisos para acceder a este recurso" });
            return context.Response.WriteAsync(result);
        }
    };
});

// Registrar servicios, repositorios y validadores
builder.Services.AddScoped<HashtagRepository>();
builder.Services.AddScoped<HashtagService>();
builder.Services.AddScoped<QuestionRepository>();
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ResponseService>();
builder.Services.AddScoped<MicrosoftAuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IValidator<CreateHashtagDto>, CreateHashtagValidator>();
builder.Services.AddScoped<IValidator<UpdateHashtagDto>, UpdateHashtagValidator>();
builder.Services.AddScoped<IValidator<CreateQuestionDto>, CreateQuestionValidator>();
builder.Services.AddScoped<CreateQuestionValidatorAsync>();
builder.Services.AddScoped<IValidator<UpdateQuestionDto>, UpdateQuestionValidator>();
builder.Services.AddScoped<IValidator<RegisterUserDto>, RegisterUserValidator>();
builder.Services.AddScoped<RegisterUserValidatorAsync>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginValidator>();

// Obtener la cadena de conexión desde el archivo .env o la configuración
string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    $"Server={Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"};" +
    $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "3306"};" +
    $"Database={Environment.GetEnvironmentVariable("DB_NAME") ?? "jool"};" +
    $"User={Environment.GetEnvironmentVariable("DB_USER") ?? "root"};" +
    $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "admin1"};";

// Add services to the container.
builder.Services.AddDbContext<JoolContext>(options =>
{
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
        Console.WriteLine("Asegúrese de que MySQL esté corriendo y la cadena de conexión sea correcta.");
        throw;
    }
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Imprimir las rutas registradas en modo desarrollo
    var endpointDataSource = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
    foreach (var endpoint in endpointDataSource.Endpoints)
    {
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            Console.WriteLine($"Ruta registrada: {routeEndpoint.RoutePattern.RawText} ({string.Join(", ", routeEndpoint.Metadata.OfType<HttpMethodMetadata>().SelectMany(m => m.HttpMethods))})");
        }
    }
}

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");

// Middleware personalizado para debug
app.Use(async (context, next) =>
{
    Console.WriteLine($"Solicitud recibida: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});

// Añadir middleware de autenticación antes de autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
