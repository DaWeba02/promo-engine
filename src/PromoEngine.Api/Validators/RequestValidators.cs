using FluentValidation;
using PromoEngine.Application.Dtos;

namespace PromoEngine.Api.Validators;

public sealed class QuoteRequestValidator : AbstractValidator<QuoteRequestDto>
{
    public QuoteRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.MinimumMarginRate).InclusiveBetween(0m, 1m);
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
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc);
        RuleFor(x => x.BudgetCap).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.BudgetConsumed).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.MinimumMarginRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.RequiredQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ChargedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetSkus).NotNull();
        RuleFor(x => x.BundleSkus).NotNull();
    }
}
