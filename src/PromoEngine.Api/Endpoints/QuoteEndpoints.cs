using FluentValidation;
using PromoEngine.Api.Extensions;
using PromoEngine.Application.Dtos;
using PromoEngine.Application.Services;

namespace PromoEngine.Api.Endpoints;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/quotes", async (
            QuoteRequestDto request,
            IValidator<QuoteRequestDto> validator,
            QuoteService quoteService,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAsync(request, cancellationToken);
            var response = await quoteService.CreateQuoteAsync(request, isSimulation: false, cancellationToken);
            return Results.Ok(response);
        });

        endpoints.MapPost("/simulate", async (
            QuoteRequestDto request,
            IValidator<QuoteRequestDto> validator,
            QuoteService quoteService,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAsync(request, cancellationToken);
            var response = await quoteService.CreateQuoteAsync(request, isSimulation: true, cancellationToken);
            return Results.Ok(response);
        });

        return endpoints;
    }
}
