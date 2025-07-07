using System.Collections.Concurrent;
using System.Linq.Expressions;
using ErrorOr;
using FastEndpoints;
using FluentValidation.Results;

namespace EliteAPI.Utilities;

public sealed class ResponseSender : IGlobalPostProcessor
{
    public Task PostProcessAsync(IPostProcessorContext ctx, CancellationToken ct)
    {
        if (ctx.HttpContext.ResponseStarted() || ctx.Response is not IErrorOr errorOr)
            return Task.CompletedTask;

        if (!errorOr.IsError)
        {
            var value = GetValueFromErrorOr(errorOr);
            // Eventually this should change to use SendCreatedAt or similar methods
            return value is Success or Created or Deleted or Updated 
                ? ctx.HttpContext.Response.SendNoContentAsync(ct) 
                : ctx.HttpContext.Response.SendAsync(value, cancellation: ct);
        }

        if (errorOr.Errors?.All(e => e.Type == ErrorType.Validation) is true)
        {
            return ctx.HttpContext.Response.SendErrorsAsync(
                failures: [..errorOr.Errors.Select(e => new ValidationFailure(e.Code, e.Description))],
                cancellation: ct);
        }

        var problem = errorOr.Errors?.FirstOrDefault(e => e.Type != ErrorType.Validation);

        return problem?.Type switch
        {
            ErrorType.Conflict => ctx.HttpContext.Response.SendAsync("Duplicate submission!", 409, cancellation: ct),
            ErrorType.NotFound => ctx.HttpContext.Response.SendNotFoundAsync(ct),
            ErrorType.Unauthorized => ctx.HttpContext.Response.SendUnauthorizedAsync(ct),
            ErrorType.Forbidden => ctx.HttpContext.Response.SendForbiddenAsync(ct),
            null => throw new InvalidOperationException(),
            _ => Task.CompletedTask
        };
    }

    // Cached compiled expressions for reading ErrorOr<T>.Value property
    private static readonly ConcurrentDictionary<Type, Func<object, object>> ValueAccessors = new();

    private static object GetValueFromErrorOr(object errorOr)
    {
        ArgumentNullException.ThrowIfNull(errorOr);
        var tErrorOr = errorOr.GetType();

        if (!tErrorOr.IsGenericType || tErrorOr.GetGenericTypeDefinition() != typeof(ErrorOr<>))
            throw new InvalidOperationException("The provided object is not an instance of ErrorOr<>.");

        return ValueAccessors.GetOrAdd(tErrorOr, CreateValueAccessor)(errorOr);

        static Func<object, object> CreateValueAccessor(Type errorOrType)
        {
            var parameter = Expression.Parameter(typeof(object), "errorOr");

            return Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(parameter, errorOrType),
                            "Value"),
                        typeof(object)),
                    parameter)
                .Compile();
        }
    }
}