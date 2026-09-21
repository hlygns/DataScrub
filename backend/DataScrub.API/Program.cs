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

// Şemayı açılışta kendisi kursun: Docker'da elle `dotnet ef database update` çalıştırılamaz.
// SQL Server container'ı healthy olsa bile ilk bağlantılar geç kalabildiği için birkaç kez deniyoruz.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Veritabanı migration'ları uygulandı.");
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning(ex, "Migration denemesi {Attempt}/10 başarısız, 3 sn sonra tekrar denenecek.", attempt);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

// --- Middleware pipeline ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Container içinde sadece HTTP:8080 dinleniyor; HTTPS yönlendirmesi CORS preflight'ı bozar.
// Resmi .NET imajları DOTNET_RUNNING_IN_CONTAINER=true set eder.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
