using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;   
using Microsoft.OpenApi.Models;                        
using System.Text;
using ExpenseTracker.API.Repositories;
using ExpenseTracker.API.Services;
using ExpenseTracker.API.Models;





var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database + repositories
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<SyncRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ExpenseRepository>();
builder.Services.AddScoped<BudgetRepository>();
builder.Services.AddScoped<ReportRepository>();

// Token + Auth
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthRepository>();

// JWT authentication
builder.Services.AddAuthentication("Bearer")
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default_secret_key_here")
            )
        };
    });

// Swagger config
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ExpenseTracker API",
        Version = "v1",
        Description = "API for Android Expense Tracker app"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker API v1");
        c.RoutePrefix = "swagger"; // ensures /swagger/index.html works
    });



// Middleware
//app.UseMiddleware<FirebaseAuthMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
