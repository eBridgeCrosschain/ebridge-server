using System;

namespace AElf.CrossChainServer.Contracts;

public class DailyLimitDto
{
    public decimal DefaultDailyLimit { get; set; }

    // Refresh Time
    public long RefreshTime { get; set; }

    // Current Daily Limit
    public decimal CurrentDailyLimit { get; set; }
}