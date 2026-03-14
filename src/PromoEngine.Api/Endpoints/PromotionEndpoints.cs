using FluentValidation;
using PromoEngine.Api.Extensions;
using PromoEngine.Application.Dtos;
using PromoEngine.Application.Services;

namespace PromoEngine.Api.Endpoints;

public static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/promotions", async (PromotionService service, CancellationToken cancellationToken)
            => Results.Ok(await service.GetAllAsync(cancellationToken)));

        endpoints.MapGet("/promotions/{id:guid}", async (Guid id, PromotionService service, CancellationToken cancellationToken)
            => await service.GetByIdAsync(id, cancellationToken) is { } promotion ? Results.Ok(promotion) : Results.NotFound());

        endpoints.MapPost("/promotions", async (
            UpsertPromotionRequest request,
            IValidator<UpsertPromotionRequest> validator,
            PromotionService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAsync(request, cancellationToken);
            var created = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/promotions/{created.Id}", created);
        });

        endpoints.MapPut("/promotions/{id:guid}", async (
            Guid id,
            UpsertPromotionRequest request,
            IValidator<UpsertPromotionRequest> validator,
            PromotionService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAsync(request, cancellationToken);
            var updated = await service.UpdateAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        endpoints.MapDelete("/promotions/{id:guid}", async (Guid id, PromotionService service, CancellationToken cancellationToken)
            => await service.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

        return endpoints;
    }
}
