using FraudGuard.API.Middleware;
using FraudGuard.API.Extensions;
using FraudGuard.Infrastructure.Cache;
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
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Infrastructure.Services;
using FraudGuard.Infrastructure.Persistence.Repositories;
using FraudGuard.Application.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FraudGuardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(cfg => 
{
    cfg.AddMaps(typeof(FraudGuard.Application.Mappings.FraudMappingProfile).Assembly);
});

builder.Services.AddScoped<ICreditCardRepository, CreditCardRepository>();
builder.Services.AddScoped<IDebitCardRepository, DebitCardRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IFraudRuleRepository, FraudRuleRepository>();
builder.Services.AddScoped<IFraudLogRepository, FraudLogRepository>();
builder.Services.AddScoped<IBankAccountBeneficiaryRepository, BankAccountBeneficiaryRepository>();
builder.Services.AddScoped<IUnitOfWork, FraudGuard.Infrastructure.Persistence.UnitOfWork>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IFraudEvaluationService, FraudEvaluationService>();
builder.Services.AddFraudRules();
builder.Services.AddScoped<IAdminOperationService, AdminOperationService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
builder.Services.AddHttpClient<ICurrencyService, TcmbCurrencyService>();
builder.Services.AddScoped<ITransactionAppService, TransactionAppService>();
builder.Services.AddScoped<IFraudManagementAppService, FraudManagementAppService>();
builder.Services.AddScoped<IRuleManagementAppService, RuleManagementAppService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ICryptService, CryptService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:4000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<FraudGuardDbContext>();
    
    int maxRetries = 12;
    int delaySeconds = 5;
    bool dbCreated = false;

    for (int retry = 1; retry <= maxRetries; retry++)
    {
        try
        {
            Console.WriteLine($"Veritabanına bağlanılıyor ve tablolar oluşturuluyor (Deneme {retry}/{maxRetries})...");
            
            // Database exists empty check
            if (!context.Database.CanConnect())
            {
                context.Database.EnsureCreated();
            }
            else
            {
                var creator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>(context.Database) as Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator;
                if (creator != null && !creator.HasTables())
                {
                    creator.CreateTables();
                }
            }

            Console.WriteLine("Veritabanı ve tablolar başarıyla oluşturuldu!");
            dbCreated = true;
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Veritabanı oluşturulurken hata (Deneme {retry} başarısız): {ex.Message}");
            if (retry < maxRetries)
            {
                Console.WriteLine($"{delaySeconds} saniye sonra tekrar denenecek...");
                System.Threading.Thread.Sleep(delaySeconds * 1000);
            }
        }
    }

    if (!dbCreated)
    {
        Console.WriteLine($"HATA: Veritabanı {maxRetries} deneme sonrasında oluşturulamadı.");
    }
}
app.UseRouting();
app.UseCors("AllowReactApp");
app.UseMiddleware<JwtAuthMiddleware>();

app.MapHub<FraudHub>("/fraudHub");

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();