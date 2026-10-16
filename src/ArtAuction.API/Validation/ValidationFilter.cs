using FluentValidation;

namespace ArtAuction.API.Validation
{
    public class ValidationFilter<T> : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

            if (validator is not null)
            {
                var argument = context.Arguments.OfType<T>().FirstOrDefault();

                if (argument is not null)
                {
                    var result = await validator.ValidateAsync(argument);

                    if (!result.IsValid)
                    {
                        return TypedResults.ValidationProblem(result.ToDictionary());
                    }
                }
            }

            return await next(context);
        }
    }
}
