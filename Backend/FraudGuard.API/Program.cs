using FraudGuard.API.Middleware;
using FraudGuard.Infrastructure.Cache;
using FraudGuard.Infrastructure.Persistence.Contexts;
using FraudGuard.Infrastructure.Persistence.Repositories;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Services.RuleEngine;
using FraudGuard.Infrastructure.RuleEngine;
using FraudGuard.Infrastructure.Diagnostics;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using FraudGuard.API.Hubs;
using FraudGuard.Infrastructure.Services;


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
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IRuleCombinationRepository, RuleCombinationRepository>();
builder.Services.AddScoped<IMerchantRepository, MerchantRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IFraudEvaluationService, FraudEvaluationService>();

// --- Dinamik kural motoru ---
// Derleyici singleton: derlenen delegate önbelleği süreç ömrü boyunca paylaşılır.
builder.Services.AddSingleton<IRuleExpressionCompiler, DynamicExpressoRuleCompiler>();
// Bozuk kurallar sessizce atlanmaz; teşhis kanalı üzerinden loglanır.
builder.Services.AddSingleton<IRuleDiagnostics, RuleDiagnostics>();
// Referans verisi (BIN tablosu, operasyonel listeler) süreç belleğinde hazır tutulur;
// her işlemde veritabanından okunup sözlüğe çevrilmesi gereksiz maliyetti.
builder.Services.AddSingleton<IReferenceDataProvider, CachedReferenceDataProvider>();
// Kombinasyon ve güven servisleri durumsuzdur.
builder.Services.AddSingleton<ICombinationEngine, CombinationEngine>();
builder.Services.AddSingleton<ITrustScoreService, TrustScoreService>();

// Saf hesaplama servisleri durumsuzdur; singleton olmaları güvenlidir.
builder.Services.AddSingleton<IRiskScoringService, RiskScoringService>();
// Motor durumsuzdur: derlenmiş ifadeleri çalıştırır, veriye erişmez.
builder.Services.AddScoped<IDynamicRuleEngine, DynamicRuleEngine>();

builder.Services.AddScoped<IAdminOperationService, AdminOperationService>();
// Önbellek Redis'te tutulur: süreç dışında olduğu için birden fazla API örneği aynı
// veriyi görür. Bir kartın bloke edilmesi tüm örneklerde anında geçerli olur; süreç içi
// bellekte bu, TTL dolana kadar mümkün değildi.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "fraudguard:";
});
builder.Services.AddSingleton<ICacheProvider, RedisCacheProvider>();
builder.Services.AddHttpClient<ICurrencyService, TcmbCurrencyService>();
builder.Services.AddScoped<ITransactionAppService, TransactionAppService>();
builder.Services.AddScoped<IFraudManagementAppService, FraudManagementAppService>();
builder.Services.AddScoped<IRuleManagementAppService, RuleManagementAppService>();
builder.Services.AddScoped<IMerchantAppService, MerchantAppService>();
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
        policy.SetIsOriginAllowed(origin => true) // 👈 IP veya localhost fark etmeksizin mobil erişime izin verir
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

            // Schema migration check for new dynamic rule columns and tables
            try
            {
                context.Database.ExecuteSqlRaw(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CreditCardTransactions') AND name = 'RiskScore')
    ALTER TABLE CreditCardTransactions ADD RiskScore INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CreditCardTransactions') AND name = 'RiskDecision')
    ALTER TABLE CreditCardTransactions ADD RiskDecision INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DebitCardTransactions') AND name = 'RiskScore')
    ALTER TABLE DebitCardTransactions ADD RiskScore INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DebitCardTransactions') AND name = 'RiskDecision')
    ALTER TABLE DebitCardTransactions ADD RiskDecision INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TransferTransactions') AND name = 'RiskScore')
    ALTER TABLE TransferTransactions ADD RiskScore INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TransferTransactions') AND name = 'RiskDecision')
    ALTER TABLE TransferTransactions ADD RiskDecision INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FraudRules') AND name = 'Expression')
    ALTER TABLE FraudRules ADD Expression NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FraudRules') AND name = 'Score')
    ALTER TABLE FraudRules ADD Score INT NOT NULL DEFAULT 20;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FraudRules') AND name = 'Target')
    ALTER TABLE FraudRules ADD Target INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FraudRules') AND name = 'Category')
    ALTER TABLE FraudRules ADD Category INT NOT NULL DEFAULT 0;

-- Gerekçe özeti 250 karaktere sığmıyordu; çok kural tetikleyen işlem kayıt sırasında hata veriyordu.
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CreditCardTransactions'
           AND COLUMN_NAME = 'FraudReason' AND CHARACTER_MAXIMUM_LENGTH < 500)
    ALTER TABLE CreditCardTransactions ALTER COLUMN FraudReason NVARCHAR(500) NULL;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DebitCardTransactions'
           AND COLUMN_NAME = 'FraudReason' AND CHARACTER_MAXIMUM_LENGTH < 500)
    ALTER TABLE DebitCardTransactions ALTER COLUMN FraudReason NVARCHAR(500) NULL;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TransferTransactions'
           AND COLUMN_NAME = 'FraudReason' AND CHARACTER_MAXIMUM_LENGTH < 500)
    ALTER TABLE TransferTransactions ALTER COLUMN FraudReason NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Merchants') AND name = 'ForeignCardsBlocked')
    ALTER TABLE Merchants ADD ForeignCardsBlocked BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Merchants') AND name = 'IsPaymentFacilitatorSub')
    ALTER TABLE Merchants ADD IsPaymentFacilitatorSub BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Merchants') AND name = 'IsTaxpayer')
    ALTER TABLE Merchants ADD IsTaxpayer BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Merchants') AND name = 'OwnerBirthDate')
    ALTER TABLE Merchants ADD OwnerBirthDate DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FraudRules') AND name = 'IsCritical')
BEGIN
    ALTER TABLE FraudRules ADD IsCritical BIT NOT NULL DEFAULT 0;
    -- Deterministik yaptırım kuralları güven indiriminden muaf tutulur.
    EXEC('UPDATE FraudRules SET IsCritical = 1 WHERE RuleCode = ''HIGH_RISK_RECEIVER''');
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RuleCombinations')
BEGIN
    CREATE TABLE RuleCombinations (
        CombinationId INT IDENTITY(1,1) PRIMARY KEY,
        CombinationName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        RuleCodes NVARCHAR(200) NOT NULL,
        BonusScore INT NOT NULL,
        Target INT NOT NULL,
        Category INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BinRanges')
BEGIN
    CREATE TABLE BinRanges (
        BinPrefix NVARCHAR(6) NOT NULL PRIMARY KEY,
        CountryCode NVARCHAR(2) NOT NULL,
        Scheme NVARCHAR(30) NOT NULL,
        BankName NVARCHAR(120) NULL,
        IsRisky BIT NOT NULL DEFAULT 0,
        IsSanctioned BIT NOT NULL DEFAULT 0,
        IsExpedia BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReferenceListEntries')
BEGIN
    CREATE TABLE ReferenceListEntries (
        EntryId INT IDENTITY(1,1) PRIMARY KEY,
        ListType NVARCHAR(40) NOT NULL,
        Value NVARCHAR(60) NOT NULL,
        Description NVARCHAR(200) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX IX_ReferenceListEntries_ListType_Value
        ON ReferenceListEntries (ListType, Value);
END
");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Şema senkronizasyonu uyarısı: {ex.Message}");
            }

            // Kural kataloğu uzlaştırması.
            // EnsureCreated yalnızca boş veritabanında seed uygular; mevcut bir kuruluma
            // sonradan eklenen seed kuralları başka türlü hiç ulaşmaz. Burada yalnızca
            // RuleCode'u bulunmayan kurallar eklenir — mevcut kayıtlara dokunulmaz, böylece
            // kullanıcının puan/aktiflik değişiklikleri her açılışta geri alınmaz.
            try
            {
                var existingCodes = context.FraudRules
                    .Select(r => r.RuleCode)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingRules = FraudGuard.Infrastructure.Persistence.SeedData.FraudRuleSeedData
                    .GetRules()
                    .Where(r => !existingCodes.Contains(r.RuleCode))
                    .ToList();

                if (missingRules.Count > 0)
                {
                    // Kimlik kolonu veritabanınca üretilsin; iş anahtarı RuleCode'dur.
                    foreach (var rule in missingRules)
                        rule.RuleId = 0;

                    context.FraudRules.AddRange(missingRules);
                    context.SaveChanges();

                    Console.WriteLine(
                        $"Kural kataloğuna {missingRules.Count} yeni kural eklendi: " +
                        string.Join(", ", missingRules.Select(r => r.RuleCode)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kural uzlaştırma uyarısı: {ex.Message}");
            }

            // Referans verisi uzlaştırması (BIN tablosu ve operasyonel listeler).
            // Kural kataloğuyla aynı gerekçe: EnsureCreated bu satırları mevcut bir
            // veritabanına eklemez. Eksik olan eklenir, var olana dokunulmaz.
            try
            {
                var existingBins = context.BinRanges
                    .Select(b => b.BinPrefix)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingBins = FraudGuard.Infrastructure.Persistence.SeedData.ReferenceDataSeed
                    .GetBinRanges()
                    .Where(b => !existingBins.Contains(b.BinPrefix))
                    .ToList();

                var existingEntries = context.ReferenceListEntries
                    .Select(e => e.ListType + "|" + e.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingEntries = FraudGuard.Infrastructure.Persistence.SeedData.ReferenceDataSeed
                    .GetListEntries()
                    .Where(e => !existingEntries.Contains(e.ListType + "|" + e.Value))
                    .ToList();

                if (missingBins.Count > 0)
                    context.BinRanges.AddRange(missingBins);

                if (missingEntries.Count > 0)
                {
                    foreach (var entry in missingEntries)
                        entry.EntryId = 0;

                    context.ReferenceListEntries.AddRange(missingEntries);
                }

                if (missingBins.Count > 0 || missingEntries.Count > 0)
                {
                    context.SaveChanges();
                    Console.WriteLine(
                        $"Referans verisi eklendi: {missingBins.Count} BIN, {missingEntries.Count} liste kaydı.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Referans verisi uzlaştırma uyarısı: {ex.Message}");
            }

            // İşyeri uzlaştırması. Yalnızca MerchantId'si bulunmayan işyerleri eklenir;
            // mevcut kayıtların alanlarına dokunulmaz çünkü işyeri master verisi operasyon
            // tarafından düzenlenebilir — seed'i her açılışta üzerine yazmak, elle yapılan
            // düzeltmeleri geri alırdı.
            try
            {
                var existingMerchants = context.Merchants
                    .Select(m => m.MerchantId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingMerchants = FraudGuard.Infrastructure.Persistence.SeedData.MerchantSeedData
                    .GetMerchants()
                    .Where(m => !existingMerchants.Contains(m.MerchantId))
                    .ToList();

                if (missingMerchants.Count > 0)
                {
                    context.Merchants.AddRange(missingMerchants);
                    context.SaveChanges();
                    Console.WriteLine(
                        $"İşyeri kataloğuna {missingMerchants.Count} yeni kayıt eklendi: " +
                        string.Join(", ", missingMerchants.Select(m => m.MerchantId)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İşyeri uzlaştırma uyarısı: {ex.Message}");
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