using FraudGuard.API.Middleware;
using FraudGuard.Infrastructure.Persistence.Contexts;
using FraudGuard.Infrastructure.Persistence.Repositories;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Services;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using FraudGuard.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FraudGuardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(cfg => 
{
    cfg.AddMaps(typeof(FraudGuard.Application.Mappings.FraudMappingProfile).Assembly);
});

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IFraudRuleRepository, FraudRuleRepository>();
builder.Services.AddScoped<IFraudLogRepository, FraudLogRepository>();
builder.Services.AddScoped<IUnitOfWork, FraudGuard.Infrastructure.Persistence.UnitOfWork>();

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IFraudEvaluationService, FraudEvaluationService>();
builder.Services.AddScoped<IAdminOperationService, AdminOperationService>();

builder.Services.AddScoped<ITransactionAppService, TransactionAppService>();
builder.Services.AddScoped<IFraudManagementAppService, FraudManagementAppService>();
builder.Services.AddScoped<IRuleManagementAppService, RuleManagementAppService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 
builder.Services.AddSignalR();

// 🔴 SİGNALR İÇİN DEĞİŞTİRİLEN KISIM
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Sadece React arayüzüne izin veriyoruz
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR'ın çalışması için kimlik onayı zorunludur
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FraudGuardDbContext>();
// Migrate yerine EnsureCreated kullanıyoruz. Bu komut modellere bakıp tüm tabloları anında yaratır.
        context.Database.EnsureCreated(); 
        Console.WriteLine("🟢 Veritabanı ve tablolar kalıcı olarak oluşturuldu!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("🔴 Veritabanı oluşturulurken hata: " + ex.Message);
    }
}
// 🔴 EKLENEN KISIM: Yönlendirme ve Cors sırası SignalR için çok önemlidir
app.UseRouting();
app.UseCors("AllowReactApp");

app.MapHub<FraudHub>("/fraudHub");

// Kalkanlar (Middleware)
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();