using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NeonMS.Mvc;

public class CustomExceptionFilter : IActionFilter, IOrderedFilter
{
    // Let other action filters run before this one
    public int Order => int.MaxValue - 10;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Return binding errors
            context.Result = new ObjectResult(new ValidationProblemDetails(context.ModelState))
            {
                StatusCode = 400,
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is ValidationException validationException)
        {
            var errors = new Dictionary<string, string[]>()
            {
                { validationException.ValidationResult.MemberNames.FirstOrDefault() ?? "", new[]{ validationException.ValidationResult.ErrorMessage ?? "Unexpected error."}}
            };
            context.Result = new ObjectResult(new ValidationProblemDetails(errors))
            {
                StatusCode = 400,
            };

            context.ExceptionHandled = true;
        }
        else if (context.Exception is not null)
        {

        }
    }
}
