using FluentValidation;
using PromoEngine.Application.Dtos;
using PromoEngine.Domain.Enums;

namespace PromoEngine.Api.Validators;

public sealed class QuoteRequestValidator : AbstractValidator<QuoteRequestDto>
{
    public QuoteRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Strategy).IsInEnum();
        RuleFor(x => x.MinimumMarginRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Channel).IsInEnum().When(x => x.Channel.HasValue);
        RuleFor(x => x.Segment).IsInEnum().When(x => x.Segment.HasValue);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new QuoteLineRequestValidator());
    }
}

public sealed class SimulationCompareRequestValidator : AbstractValidator<SimulationCompareRequestDto>
{
    public SimulationCompareRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.MinimumMarginRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Channel).IsInEnum().When(x => x.Channel.HasValue);
        RuleFor(x => x.Segment).IsInEnum().When(x => x.Segment.HasValue);
        RuleFor(x => x.Strategies).NotEmpty();
        RuleForEach(x => x.Strategies).IsInEnum();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new QuoteLineRequestValidator());
    }
}

public sealed class QuoteLineRequestValidator : AbstractValidator<QuoteLineRequestDto>
{
    public QuoteLineRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0m);
    }
}

public sealed class UpsertPromotionRequestValidator : AbstractValidator<UpsertPromotionRequest>
{
    public UpsertPromotionRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.CampaignKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.DiscountValueType).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum().When(x => x.Channel.HasValue);
        RuleFor(x => x.Segment).IsInEnum().When(x => x.Segment.HasValue);
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc);
        RuleFor(x => x.BudgetCap).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.BudgetConsumed).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.BudgetDailyCap).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.BudgetPerCustomerCap).GreaterThanOrEqualTo(0m).When(x => x.BudgetPerCustomerCap.HasValue);
        RuleFor(x => x.MinimumMarginRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.MinimumCartValue).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MaximumDiscount).GreaterThan(0m).When(x => x.MaximumDiscount.HasValue);
        RuleFor(x => x.FundingManufacturerRate).InclusiveBetween(0m, 1m).When(x => x.FundingManufacturerRate.HasValue);
        RuleFor(x => x.FundingRetailerRate).InclusiveBetween(0m, 1m).When(x => x.FundingRetailerRate.HasValue);
        RuleFor(x => x.RequiredQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ChargedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetSkus).NotNull();
        RuleFor(x => x.BundleSkus).NotNull();

        RuleFor(x => x).Custom((request, context) =>
        {
            var manufacturerRate = request.FundingManufacturerRate
                ?? (request.FundingRetailerRate.HasValue ? 1m - request.FundingRetailerRate.Value : request.IsFunded ? 0.5m : 0m);
            var retailerRate = request.FundingRetailerRate
                ?? (request.FundingManufacturerRate.HasValue ? 1m - request.FundingManufacturerRate.Value : request.IsFunded ? 0.5m : 1m);

            if (decimal.Round(manufacturerRate + retailerRate, 4, MidpointRounding.AwayFromZero) != 1m)
            {
                context.AddFailure(nameof(request.FundingManufacturerRate), "Funding manufacturer and retailer rates must sum to 1.0.");
            }

            if (request.Type == PromotionType.QuantityDeal && request.ChargedQuantity > request.RequiredQuantity)
            {
                context.AddFailure(nameof(request.ChargedQuantity), "Charged quantity cannot exceed required quantity.");
            }
        });
    }
}
