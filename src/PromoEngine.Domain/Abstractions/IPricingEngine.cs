using PromoEngine.Domain.Models;

namespace PromoEngine.Domain.Abstractions;

public interface IPricingEngine
{
    PriceQuote Evaluate(QuoteRequest request, IReadOnlyCollection<Promotion> promotions, DateTimeOffset now, bool isSimulation);
}
