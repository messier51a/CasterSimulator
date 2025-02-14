using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ✅ Enable CORS to allow requests from Grafana
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGrafana",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Allow only Grafana
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// ✅ Load Kestrel configuration from appsettings.json
var kestrelConfig = builder.Configuration.GetSection("Kestrel");
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(kestrelConfig);
});

builder.Services.AddControllers();

// ✅ Add Swagger Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ Apply CORS Policy
app.UseCors("AllowGrafana");

app.UseRouting();
app.UseAuthorization();

// ✅ Enable Swagger in Development Mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();