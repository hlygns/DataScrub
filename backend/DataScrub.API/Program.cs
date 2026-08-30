using System;
using DataScrub.Application.Interfaces;
using DataScrub.Application.Services;
using DataScrub.Infrastructure.Data;
using DataScrub.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Servisleri kaydet (Dependency Injection) ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository ve Application servisleri
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddScoped<DatasetService>();

// Python ML servisine konuşacak HttpClient - appsettings.json'dan base URL alıyor
builder.Services.AddHttpClient<IMlAnalysisService, MlAnalysisService>(client =>
{
    var mlServiceUrl = builder.Configuration["MlService:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(mlServiceUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // büyük dosyalar için makul bir zaman aşımı
});

// React frontend'in API'ye erişebilmesi için CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Middleware pipeline ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
