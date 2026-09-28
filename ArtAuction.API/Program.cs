using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(new Uri("sb://sb-masstransit-neu-002.servicebus.windows.net"));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();

    app.MapScalarApiReference();

//}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;

        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        var logger = context.RequestServices
         .GetRequiredService<ILogger<Program>>();

        logger.LogError(exception?.Message, exception);

        var dev = app.Environment.IsDevelopment();

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = exception.Message,
            Status = 500
        });
    });
});
app.Run();

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}
