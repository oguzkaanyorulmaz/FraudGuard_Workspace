using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.RuleManagement;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    public class RuleManagementAppService : IRuleManagementAppService
    {
        private readonly IFraudRuleRepository _fraudRuleRepository;
        private readonly IRuleExpressionCompiler _expressionCompiler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// Modelde tanımlı ama çalışma anında doldurulmayan alanlar.
        /// Merchant master verisi sisteme eklenene kadar bu alanları kullanan kurallar tetiklenmez.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> UnpopulatedFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MerchantId"] = "Merchant entity'si eklenene kadar boş",
                ["MccKodu"] = "Merchant entity'si eklenene kadar boş",
                ["FarkliKartSayisi"] = "İşyeri bazlı sayaç — Merchant verisi gerektirir",
                ["FarkliIsyeriSayisi"] = "İşyeri bazlı sayaç — Merchant verisi gerektirir",
                ["PosTahsisTarihi"] = "Merchant entity'si eklenene kadar boş"
            };

        public RuleManagementAppService(
            IFraudRuleRepository fraudRuleRepository,
            IRuleExpressionCompiler expressionCompiler,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _fraudRuleRepository = fraudRuleRepository;
            _expressionCompiler = expressionCompiler;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResponseDTO<List<GetActiveRulesResponse>>> GetActiveRulesAsync()
        {
            var rules = await _fraudRuleRepository.GetAllActiveRulesAsync();
            return ResponseDTO<List<GetActiveRulesResponse>>.Success(
                _mapper.Map<List<GetActiveRulesResponse>>(rules));
        }

        public async Task<ResponseDTO<List<GetActiveRulesResponse>>> GetAllRulesAsync()
        {
            var rules = await _fraudRuleRepository.GetAllAsync();
            return ResponseDTO<List<GetActiveRulesResponse>>.Success(
                _mapper.Map<List<GetActiveRulesResponse>>(rules));
        }

        public Task<ResponseDTO<ValidateExpressionResponse>> ValidateExpressionAsync(
            ValidateExpressionRequest request)
        {
            bool isValid = _expressionCompiler.TryValidate(request.Expression, out var error);

            var response = new ValidateExpressionResponse
            {
                IsValid = isValid,
                Error = error
            };

            return Task.FromResult(ResponseDTO<ValidateExpressionResponse>.Success(
                response,
                isValid ? "İfade geçerli." : "İfade derlenemedi."));
        }

        public async Task<ResponseDTO<CreateFraudRuleResponse>> CreateRuleAsync(CreateFraudRuleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RuleCode))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Kural kodu zorunludur.");

            if (string.IsNullOrWhiteSpace(request.RuleName))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Kural adı zorunludur.");

            if (string.IsNullOrWhiteSpace(request.Expression))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("İfade zorunludur.");

            if (request.Score <= 0)
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Puan 0'dan büyük olmalıdır.");

            if (!Enum.TryParse<RuleTargetEnum>(request.Target, ignoreCase: true, out var target))
                return ResponseDTO<CreateFraudRuleResponse>.Fail("Hedef 'Card' veya 'Merchant' olmalıdır.");

            if (!Enum.TryParse<RuleCategoryEnum>(request.Category, ignoreCase: true, out var category))
                return ResponseDTO<CreateFraudRuleResponse>.Fail(
                    "Kategori 'Velocity', 'Amount', 'Time', 'Identity' veya 'Location' olmalıdır.");

            // İfade derlenmeden kural kaydedilmez — sessizce ölü kural oluşmasını engeller.
            if (!_expressionCompiler.TryValidate(request.Expression, out var expressionError))
                return ResponseDTO<CreateFraudRuleResponse>.Fail($"İfade derlenemedi: {expressionError}");

            if (await _fraudRuleRepository.ExistsByCodeAsync(request.RuleCode))
                return ResponseDTO<CreateFraudRuleResponse>.Fail(
                    $"'{request.RuleCode}' kodlu bir kural zaten var.");

            var rule = new EFraudRule
            {
                RuleCode = request.RuleCode.Trim(),
                RuleName = request.RuleName.Trim(),
                Description = request.Description?.Trim(),
                Expression = request.Expression.Trim(),
                Score = request.Score,
                Target = target,
                Category = category,
                IsActive = request.IsActive
            };

            await _fraudRuleRepository.AddAsync(rule);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDTO<CreateFraudRuleResponse>.Success(
                new CreateFraudRuleResponse
                {
                    RuleId = rule.RuleId,
                    RuleCode = rule.RuleCode,
                    Message = rule.IsActive
                        ? "Kural eklendi ve bir sonraki işlemden itibaren aktif."
                        : "Kural eklendi, pasif durumda."
                },
                "Kural oluşturuldu.");
        }

        public ResponseDTO<List<RuleFieldDto>> GetAvailableFields()
        {
            var fields = typeof(ProcessTransactionInput)
                .GetProperties()
                .Where(p => p.CanRead)
                .Select(p =>
                {
                    bool unpopulated = UnpopulatedFields.TryGetValue(p.Name, out var note);
                    return new RuleFieldDto
                    {
                        Name = p.Name,
                        Type = FormatTypeName(p.PropertyType),
                        IsPopulated = !unpopulated,
                        Note = note
                    };
                })
                .OrderByDescending(f => f.IsPopulated)
                .ThenBy(f => f.Name)
                .ToList();

            return ResponseDTO<List<RuleFieldDto>>.Success(fields);
        }

        private static string FormatTypeName(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            string name = (underlying ?? type).Name;
            return underlying is null ? name : $"{name}?";
        }
    }
}
