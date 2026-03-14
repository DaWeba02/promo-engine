namespace PromoEngine.Domain.ValueObjects;

internal static class Money
{
    public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static IReadOnlyDictionary<int, decimal> Allocate(decimal total, IReadOnlyDictionary<int, decimal> weights)
    {
        if (weights.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var roundedTotal = Round(total);
        var sumWeights = weights.Values.Sum();
        if (roundedTotal == 0m || sumWeights <= 0m)
        {
            return weights.Keys.ToDictionary(key => key, _ => 0m);
        }

        var allocation = new Dictionary<int, decimal>(weights.Count);
        var remaining = roundedTotal;
        var pairs = weights.OrderBy(x => x.Key).ToArray();

        for (var index = 0; index < pairs.Length; index++)
        {
            var pair = pairs[index];
            var amount = index == pairs.Length - 1
                ? remaining
                : Round(roundedTotal * (pair.Value / sumWeights));

            allocation[pair.Key] = amount;
            remaining = Round(remaining - amount);
        }

        return allocation;
    }
}
