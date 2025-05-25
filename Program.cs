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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<CreateHashtagValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
});

// Registrar servicios, repositorios y validadores
builder.Services.AddScoped<HashtagRepository>();
builder.Services.AddScoped<HashtagService>();
builder.Services.AddScoped<QuestionRepository>();
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();

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

// Añadir middleware de autenticación antes de autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
