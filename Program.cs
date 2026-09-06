using Microsoft.OpenApi.Models;
using ExpenseTracker.API.Middleware;
using ExpenseTracker.API.Repositories;
using ExpenseTracker.API.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS for Flutter app and Web clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Forwarded Headers for Cloudflare Tunnel / Reverse Proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExpenseTracker API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
    c.OperationFilter<ExpenseTracker.API.Helpers.AuthResponsesOperationFilter>();
});

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddSingleton<TokenService>(); // Register TokenService

// Repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<BudgetRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<ExpenseRepository>();
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<SyncRepository>();

var app = builder.Build();

app.UseForwardedHeaders();

// Initialize the database tables if they don't exist
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    try {
        initializer.Initialize();
    } catch (System.Exception ex) {
        System.Console.WriteLine("Could not initialize DB: " + ex.Message);
    }
}

// Enable Swagger UI for all environments (including production and tunnels)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseTracker API v1");
    c.RoutePrefix = "swagger"; 
});

app.UseCors("AllowAll");

app.UseMiddleware<FirebaseAuthMiddleware>();

app.UseAuthorization();

// Health check endpoints for Flutter app and uptime monitors
app.MapGet("/", () => Results.Ok(new { status = "healthy", message = "ExpenseTracker API is running" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();

