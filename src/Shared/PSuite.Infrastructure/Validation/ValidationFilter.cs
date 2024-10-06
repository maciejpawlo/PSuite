using System;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSuite.Shared.Infrastructure.Validation;

public class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    private readonly IValidator<TRequest> validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().First();

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithFluentValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
        return builder;
    }
}