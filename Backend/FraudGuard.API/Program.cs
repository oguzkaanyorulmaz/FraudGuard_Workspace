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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();


app.UseCors("AllowAllOrigins");
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