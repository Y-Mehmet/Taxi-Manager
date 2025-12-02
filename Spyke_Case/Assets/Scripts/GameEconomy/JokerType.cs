/// <summary>
/// Joker types for the economy system
/// </summary>
public enum JokerType
{
    None = 0,
    
    // Tax Reduction Jokers
    DoubleBookkeeping = 1,      // 10 Stars - 10% tax for 10 sessions
    Bribery = 2,                // 10 Stars - 0% tax for 5 sessions
    HighOperatingExpenses = 3,  // 30 Stars - 0% tax for 20 sessions
    OffshoreAccounts = 4,       // 100 Stars - Unlimited 5% tax
    
    // Collision/Repair Jokers
    CollisionInsurance = 5,     // 10 Stars - Zero repair for 5 sessions
    OwnRepairStation = 6        // 100 Stars - Unlimited 100 coin fixed repair
}
