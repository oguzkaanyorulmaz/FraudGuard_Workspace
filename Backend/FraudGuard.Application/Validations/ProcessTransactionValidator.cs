using FluentValidation;
using FraudGuard.Application.DTOs.TransactionProcessing;

namespace FraudGuard.Application.Validations
{
    public class ProcessTransactionValidator : AbstractValidator<ProcessTransactionRequest>
    {
        public ProcessTransactionValidator()
        {
            RuleFor(x => x.CardNumber).NotEmpty().Length(16).WithMessage("Kart numarası 16 haneli olmalıdır.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("İşlem tutarı 0'dan büyük olmalıdır.");
            RuleFor(x => x.CVV).NotEmpty().Length(3).WithMessage("CVV 3 haneli olmalıdır.");
            RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Para birimi 3 karakter olmalıdır (Örn: TRY).");
            RuleFor(x => x.Location).NotEmpty().WithMessage("Lokasyon bilgisi zorunludur.");
        }
    }
}