using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Ordering.Domain; // Тут мають лежати твої NotFoundException, BusinessConflictException

namespace Ordering.Api;

public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        // 1. Призначаємо правильний HTTP-статус залежно від типу помилки
        http.Response.StatusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessConflictException => StatusCodes.Status409Conflict,
            System.ComponentModel.DataAnnotations.ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        // 2. Формуємо стандартну відповідь ProblemDetails (RFC 7807)
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext 
        { 
            HttpContext = http, 
            Exception = ex 
        });
    }
}

