using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Ordering.Domain; 

namespace Ordering.Api;

public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        // Якщо це не наша помилка - віддаємо системі (буде 500)
        if (ex is not NotFoundException and not BusinessConflictException and not ValidationException)
        {
            return false; 
        }

        http.Response.StatusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessConflictException => StatusCodes.Status409Conflict,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        await problemDetails.TryWriteAsync(new ProblemDetailsContext 
        { 
            HttpContext = http, 
            Exception = ex 
        });

        return true; 
    }
}