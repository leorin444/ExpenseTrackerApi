using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExpenseTracker.API.Helpers
{
    public class AuthResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var allowAnonymous = (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>().Any() ?? false) ||
                context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

            if (allowAnonymous)
            {
                operation.Security.Clear();
            }
        }
    }
}
