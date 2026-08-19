using FraudGuard.Domain.Common.Constants;
using FluentValidation;
using FraudGuard.Application.DTOs.FraudManagement;

namespace FraudGuard.Application.Validations
{
    public class ResolveFraudLogValidator : AbstractValidator<ResolveFraudLogRequest>
    {
        public ResolveFraudLogValidator()
        {
            RuleFor(x => x.LogId).GreaterThan(0).WithMessage("Geçerli bir Log ID girilmelidir.");
            
            RuleFor(x => x.AdminAction)
                .NotEmpty()
                .Must(action => action == AdminActions.Approved || action == AdminActions.CardBlocked)
                .WithMessage("Aksiyon sadece 'Approved' veya 'CardBlocked' olabilir.");
        }
    }
}