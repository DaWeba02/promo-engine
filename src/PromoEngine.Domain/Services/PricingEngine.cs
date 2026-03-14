using PromoEngine.Domain.Abstractions;
using PromoEngine.Domain.Enums;
using PromoEngine.Domain.Models;
using PromoEngine.Domain.ValueObjects;

namespace PromoEngine.Domain.Services;

public sealed class PricingEngine : IPricingEngine
{
    public PriceQuote Evaluate(QuoteRequest request, IReadOnlyCollection<Promotion> promotions, DateTimeOffset now, bool isSimulation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(promotions);

        var subtotal = Money.Round(request.Items.Sum(item => item.Quantity * item.UnitPrice));
        var costTotal = Money.Round(request.Items.Sum(item => item.Quantity * item.UnitCost));
        var initialMarginAmount = Money.Round(subtotal - costTotal);
        var decisions = new List<PromotionDecision>();
        var candidates = new List<EvaluatedPromotion>();

        foreach (var promotion in promotions)
        {
            var evaluation = EvaluatePromotion(request, promotion, now);
            if (evaluation.Candidate is null)
            {
                decisions.Add(evaluation.Decision);
                continue;
            }

            candidates.Add(evaluation.Candidate);
        }

        var runningDiscount = 0m;
        var selected = new List<EvaluatedPromotion>();
        var reservedLines = new HashSet<int>();

        foreach (var candidate in OrderCandidates(candidates, request.Strategy, subtotal, initialMarginAmount))
        {
            if (candidate.AffectedLineIndexes.Any(reservedLines.Contains))
            {
                decisions.Add(candidate.ToRejectedDecision("ConflictStrategyRejected"));
                continue;
            }

            var projectedDiscount = Money.Round(runningDiscount + candidate.TotalDiscount);
            var projectedNet = Money.Round(subtotal - projectedDiscount);
            var projectedMargin = Money.Round(projectedNet - costTotal);
            var projectedMarginRate = projectedNet <= 0m ? 0m : Math.Max(0m, projectedMargin / projectedNet);
            var requiredMarginRate = Math.Max(request.MinimumMarginRate, candidate.Promotion.MinimumMarginRate);
            if (projectedMarginRate < requiredMarginRate)
            {
                decisions.Add(candidate.ToRejectedDecision("MarginGuardRejected"));
                continue;
            }

            if (candidate.BudgetImpact > candidate.Promotion.BudgetRemaining)
            {
                decisions.Add(candidate.ToRejectedDecision("BudgetExceeded"));
                continue;
            }

            selected.Add(candidate);
            runningDiscount = projectedDiscount;
            foreach (var lineIndex in candidate.AffectedLineIndexes)
            {
                reservedLines.Add(lineIndex);
            }

            decisions.Add(candidate.ToAppliedDecision());
        }

        var lineDiscounts = new Dictionary<int, decimal>();
        foreach (var candidate in selected)
        {
            foreach (var lineDiscount in candidate.LineDiscounts)
            {
                lineDiscounts[lineDiscount.Key] = Money.Round(lineDiscounts.GetValueOrDefault(lineDiscount.Key) + lineDiscount.Value);
            }
        }

        var lines = request.Items
            .Select((line, index) =>
            {
                var lineSubtotal = Money.Round(line.Quantity * line.UnitPrice);
                var discount = Money.Round(lineDiscounts.GetValueOrDefault(index));
                return new QuotedLine(
                    line.Sku,
                    line.Quantity,
                    Money.Round(line.UnitPrice),
                    Money.Round(line.UnitCost),
                    lineSubtotal,
                    discount,
                    Money.Round(lineSubtotal - discount));
            })
            .ToArray();

        var discountTotal = Money.Round(lines.Sum(line => line.DiscountAmount));
        var netTotal = Money.Round(subtotal - discountTotal);
        var marginAmount = Money.Round(netTotal - costTotal);
        var marginRate = netTotal <= 0m ? 0m : Math.Max(0m, marginAmount / netTotal);
        var kpiSummary = new KpiImpact(
            RevenueDelta: Money.Round(-discountTotal),
            MarginDelta: Money.Round(-discountTotal),
            InventoryScore: Money.Round(selected.Sum(candidate => candidate.InventoryScore)),
            BudgetUsage: Money.Round(selected.Sum(candidate => candidate.BudgetImpact)));

        return new PriceQuote(
            QuoteId: Guid.NewGuid(),
            IsSimulation: isSimulation,
            Currency: request.Currency,
            Strategy: request.Strategy,
            Subtotal: subtotal,
            DiscountTotal: discountTotal,
            NetTotal: netTotal,
            MarginAmount: marginAmount,
            MarginRate: marginRate,
            Lines: lines,
            Promotions: decisions.OrderBy(x => x.PromotionCode, StringComparer.OrdinalIgnoreCase).ToArray(),
            KpiSummary: kpiSummary);
    }

    private static IEnumerable<EvaluatedPromotion> OrderCandidates(
        IEnumerable<EvaluatedPromotion> candidates,
        ConflictResolutionStrategy strategy,
        decimal subtotal,
        decimal initialMarginAmount)
    {
        return strategy switch
        {
            ConflictResolutionStrategy.MarginFirst => candidates
                .OrderByDescending(candidate => subtotal <= 0m ? 0m : (initialMarginAmount - candidate.TotalDiscount) / Math.Max(0.01m, subtotal - candidate.TotalDiscount))
                .ThenByDescending(candidate => candidate.Promotion.Priority)
                .ThenBy(candidate => candidate.Promotion.Code, StringComparer.OrdinalIgnoreCase),
            ConflictResolutionStrategy.FundedPromotionPreferred => candidates
                .OrderByDescending(candidate => candidate.Promotion.IsFunded)
                .ThenByDescending(candidate => candidate.TotalDiscount)
                .ThenByDescending(candidate => candidate.Promotion.Priority)
                .ThenBy(candidate => candidate.Promotion.Code, StringComparer.OrdinalIgnoreCase),
            ConflictResolutionStrategy.InventoryReduction => candidates
                .OrderByDescending(candidate => candidate.InventoryScore)
                .ThenByDescending(candidate => candidate.TotalDiscount)
                .ThenByDescending(candidate => candidate.Promotion.Priority)
                .ThenBy(candidate => candidate.Promotion.Code, StringComparer.OrdinalIgnoreCase),
            ConflictResolutionStrategy.CampaignPriority => candidates
                .OrderByDescending(candidate => candidate.Promotion.Priority)
                .ThenByDescending(candidate => candidate.TotalDiscount)
                .ThenBy(candidate => candidate.Promotion.Code, StringComparer.OrdinalIgnoreCase),
            _ => candidates
                .OrderByDescending(candidate => candidate.TotalDiscount)
                .ThenByDescending(candidate => candidate.Promotion.Priority)
                .ThenBy(candidate => candidate.Promotion.Code, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static PromotionEvaluation EvaluatePromotion(QuoteRequest request, Promotion promotion, DateTimeOffset now)
    {
        if (!promotion.IsActive)
        {
            return PromotionEvaluation.Rejected(promotion, "Inactive");
        }

        if (promotion.StartsAtUtc > now || promotion.EndsAtUtc < now)
        {
            return PromotionEvaluation.Rejected(promotion, "OutsideValidityWindow");
        }

        if (promotion.Type == PromotionType.Coupon && !string.Equals(promotion.CouponCode, request.CouponCode, StringComparison.OrdinalIgnoreCase))
        {
            return PromotionEvaluation.Rejected(promotion, "CouponMissingOrInvalid");
        }

        var targetIndexes = GetTargetIndexes(request.Items, promotion.TargetSkus);
        if (promotion.Type != PromotionType.CartDiscount && promotion.Type != PromotionType.Bundle && targetIndexes.Count == 0)
        {
            return PromotionEvaluation.Rejected(promotion, "NoEligibleItems");
        }

        return promotion.Type switch
        {
            PromotionType.PercentDiscount => EvaluatePercentDiscount(request, promotion, targetIndexes),
            PromotionType.FixedAmountDiscount => EvaluateFixedAmountDiscount(request, promotion, targetIndexes),
            PromotionType.CartDiscount => EvaluateCartDiscount(request, promotion),
            PromotionType.QuantityDeal => EvaluateQuantityDeal(request, promotion, targetIndexes),
            PromotionType.Bundle => EvaluateBundle(request, promotion),
            PromotionType.Coupon => EvaluateCoupon(request, promotion, targetIndexes),
            _ => PromotionEvaluation.Rejected(promotion, "UnsupportedPromotionType")
        };
    }

    private static PromotionEvaluation EvaluatePercentDiscount(QuoteRequest request, Promotion promotion, IReadOnlyList<int> targetIndexes)
    {
        var weights = targetIndexes.ToDictionary(index => index, index => request.Items[index].Quantity * request.Items[index].UnitPrice);
        var total = Money.Round(weights.Values.Sum() * (promotion.Value / 100m));
        return CreateCandidate(promotion, request, targetIndexes, Money.Allocate(total, weights));
    }

    private static PromotionEvaluation EvaluateFixedAmountDiscount(QuoteRequest request, Promotion promotion, IReadOnlyList<int> targetIndexes)
    {
        var discounts = targetIndexes.ToDictionary(index => index, index => Money.Round(request.Items[index].Quantity * promotion.Value));
        return CreateCandidate(promotion, request, targetIndexes, discounts);
    }

    private static PromotionEvaluation EvaluateCartDiscount(QuoteRequest request, Promotion promotion)
    {
        var subtotal = Money.Round(request.Items.Sum(item => item.Quantity * item.UnitPrice));
        if (subtotal < promotion.ThresholdAmount)
        {
            return PromotionEvaluation.Rejected(promotion, "CartThresholdNotMet");
        }

        var indexes = request.Items.Select((_, index) => index).ToArray();
        var weights = indexes.ToDictionary(index => index, index => request.Items[index].Quantity * request.Items[index].UnitPrice);
        return CreateCandidate(promotion, request, indexes, Money.Allocate(promotion.Value, weights));
    }

    private static PromotionEvaluation EvaluateQuantityDeal(QuoteRequest request, Promotion promotion, IReadOnlyList<int> targetIndexes)
    {
        var discounts = new Dictionary<int, decimal>();
        foreach (var index in targetIndexes)
        {
            var line = request.Items[index];
            var groups = promotion.RequiredQuantity <= 0 ? 0 : line.Quantity / promotion.RequiredQuantity;
            var freeUnits = groups * Math.Max(0, promotion.RequiredQuantity - promotion.ChargedQuantity);
            if (freeUnits > 0)
            {
                discounts[index] = Money.Round(freeUnits * line.UnitPrice);
            }
        }

        return discounts.Count == 0
            ? PromotionEvaluation.Rejected(promotion, "QuantityThresholdNotMet")
            : CreateCandidate(promotion, request, discounts.Keys.ToArray(), discounts);
    }

    private static PromotionEvaluation EvaluateBundle(QuoteRequest request, Promotion promotion)
    {
        var bundleSkus = promotion.BundleSkus.Where(sku => !string.IsNullOrWhiteSpace(sku)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (bundleSkus.Length == 0)
        {
            return PromotionEvaluation.Rejected(promotion, "BundleDefinitionMissing");
        }

        var matchingLines = request.Items
            .Select((item, index) => new { item, index })
            .Where(x => bundleSkus.Contains(x.item.Sku, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (matchingLines.Length != bundleSkus.Length)
        {
            return PromotionEvaluation.Rejected(promotion, "BundleRequirementsNotMet");
        }

        var bundleCount = matchingLines.Min(x => x.item.Quantity);
        if (bundleCount <= 0)
        {
            return PromotionEvaluation.Rejected(promotion, "BundleRequirementsNotMet");
        }

        var regularTotal = Money.Round(matchingLines.Sum(line => line.item.UnitPrice) * bundleCount);
        var discount = Money.Round(regularTotal - (promotion.BundlePrice * bundleCount));
        if (discount <= 0m)
        {
            return PromotionEvaluation.Rejected(promotion, "BundleNotBeneficial");
        }

        var weights = matchingLines.ToDictionary(x => x.index, x => x.item.UnitPrice * bundleCount);
        return CreateCandidate(promotion, request, matchingLines.Select(x => x.index).ToArray(), Money.Allocate(discount, weights));
    }

    private static PromotionEvaluation EvaluateCoupon(QuoteRequest request, Promotion promotion, IReadOnlyList<int> targetIndexes)
    {
        if (string.IsNullOrWhiteSpace(request.CouponCode))
        {
            return PromotionEvaluation.Rejected(promotion, "CouponMissingOrInvalid");
        }

        return promotion.DiscountValueType switch
        {
            DiscountValueType.Percentage => EvaluatePercentDiscount(request, promotion, targetIndexes),
            _ => EvaluateFixedAmountDiscount(request, promotion, targetIndexes)
        };
    }

    private static PromotionEvaluation CreateCandidate(
        Promotion promotion,
        QuoteRequest request,
        IReadOnlyList<int> lineIndexes,
        IReadOnlyDictionary<int, decimal> lineDiscounts)
    {
        var cleanedDiscounts = lineDiscounts
            .Where(x => x.Value > 0m)
            .ToDictionary(x => x.Key, x => Money.Round(x.Value));

        if (cleanedDiscounts.Count == 0)
        {
            return PromotionEvaluation.Rejected(promotion, "DiscountCalculatedAsZero");
        }

        var totalDiscount = Money.Round(cleanedDiscounts.Values.Sum());
        var inventoryScore = Money.Round(cleanedDiscounts.Keys.Sum(index => (request.Items[index].StockLevel ?? 0) * request.Items[index].Quantity));
        var impacts = cleanedDiscounts
            .Select(x => new PromotionLineImpact(request.Items[x.Key].Sku, request.Items[x.Key].Quantity, x.Value))
            .ToArray();

        return PromotionEvaluation.Accepted(new EvaluatedPromotion(
            promotion,
            cleanedDiscounts,
            lineIndexes.Distinct().OrderBy(index => index).ToArray(),
            totalDiscount,
            promotion.BudgetCap > 0m || promotion.IsFunded ? totalDiscount : 0m,
            inventoryScore,
            impacts));
    }

    private static IReadOnlyList<int> GetTargetIndexes(IReadOnlyList<QuoteLine> lines, IReadOnlyList<string> targetSkus)
    {
        if (targetSkus.Count == 0)
        {
            return lines.Select((_, index) => index).ToArray();
        }

        return lines
            .Select((line, index) => new { line.Sku, index })
            .Where(x => targetSkus.Contains(x.Sku, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.index)
            .ToArray();
    }

    private sealed record PromotionEvaluation(EvaluatedPromotion? Candidate, PromotionDecision Decision)
    {
        public static PromotionEvaluation Accepted(EvaluatedPromotion candidate) => new(candidate, candidate.ToAppliedDecision());

        public static PromotionEvaluation Rejected(Promotion promotion, string reasonCode) => new(
            null,
            new PromotionDecision(
                promotion.Id,
                promotion.Code,
                promotion.Name,
                "Rejected",
                reasonCode,
                0m,
                0m,
                Array.Empty<PromotionLineImpact>(),
                new KpiImpact(0m, 0m, 0m, 0m)));
    }

    private sealed record EvaluatedPromotion(
        Promotion Promotion,
        IReadOnlyDictionary<int, decimal> LineDiscounts,
        IReadOnlyList<int> AffectedLineIndexes,
        decimal TotalDiscount,
        decimal BudgetImpact,
        decimal InventoryScore,
        IReadOnlyList<PromotionLineImpact> AffectedItems)
    {
        public PromotionDecision ToAppliedDecision() => new(
            Promotion.Id,
            Promotion.Code,
            Promotion.Name,
            "Applied",
            "Applied",
            TotalDiscount,
            BudgetImpact,
            AffectedItems,
            new KpiImpact(-TotalDiscount, -TotalDiscount, InventoryScore, BudgetImpact));

        public PromotionDecision ToRejectedDecision(string reasonCode) => new(
            Promotion.Id,
            Promotion.Code,
            Promotion.Name,
            "Rejected",
            reasonCode,
            TotalDiscount,
            0m,
            AffectedItems,
            new KpiImpact(0m, 0m, InventoryScore, 0m));
    }
}
