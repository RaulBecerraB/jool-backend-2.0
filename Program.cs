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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<CreateHashtagValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
    "Server=localhost;Port=3306;Database=jool;User=root;Password=admin1;";

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
}

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");

// Añadir middleware de autenticación antes de autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
