namespace PromoEngine.Domain.Enums;

public enum Channel
{
    Store = 0,
    Online = 1,
    MobileApp = 2,
    ClickAndCollect = 3
}

public enum CustomerSegment
{
    NewCustomer = 0,
    ExistingCustomer = 1,
    Loyalty = 2,
    PriceSensitive = 3,
    B2B = 4,
    B2C = 5
}

public enum ConflictResolutionStrategy
{
    CustomerBestPrice = 0,
    MarginFirst = 1,
    FundedPromotionPreferred = 2,
    InventoryReduction = 3,
    CampaignPriority = 4
}

public enum PromotionType
{
    PercentDiscount = 0,
    FixedAmountDiscount = 1,
    CartDiscount = 2,
    QuantityDeal = 3,
    Bundle = 4,
    Coupon = 5
}

public enum DiscountValueType
{
    FixedAmount = 0,
    Percentage = 1
}
