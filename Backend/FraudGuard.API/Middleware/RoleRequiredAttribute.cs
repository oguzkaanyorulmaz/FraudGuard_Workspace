using FraudGuard.Domain.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FraudGuard.API.Middleware
{
    public class RoleRequired : TypeFilterAttribute
    {
        public RoleRequired(params UserRoleEnum[] requiredRoles) : base(typeof(RoleRequiredImplementation))
        {
            Arguments = new object[] { requiredRoles };
        }

        private class RoleRequiredImplementation : IAsyncActionFilter
        {
            private readonly UserRoleEnum[] _requiredRoles;

            public RoleRequiredImplementation(UserRoleEnum[] requiredRoles)
            {
                _requiredRoles = requiredRoles;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var httpContext = context.HttpContext;

                if (httpContext.Items["role"] is not UserRoleEnum userRole)
                {
                    context.Result = new UnauthorizedObjectResult("Yetkilendirme bilgisi bulunamadı.");
                    return;
                }

                if (!_requiredRoles.Contains(userRole))
                {
                    context.Result = new ObjectResult("Bu işlem için yetkiniz bulunmamaktadır.")
                    {
                        StatusCode = 403
                    };
                    return;
                }

                await next();
            }
        }
    }
}
