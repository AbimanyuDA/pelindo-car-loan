using PelindoCarLoan.API.Extensions;
using PelindoCarLoan.API.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pelindo-car-loan-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure custom services
builder.Services.ConfigureSwagger();
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureJwtAuthentication(builder.Configuration);
builder.Services.ConfigureCors(builder.Configuration);
builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureAutoMapper();
builder.Services.ConfigureValidation();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pelindo Car Loan API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("Pelindo Car Loan API started successfully");

app.Run();
