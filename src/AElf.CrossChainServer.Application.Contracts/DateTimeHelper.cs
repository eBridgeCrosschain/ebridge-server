using System;

namespace AElf.CrossChainServer;

public class DateTimeHelper
{
    public static long ToUnixTimeMilliseconds(DateTime value)
    {
        var span = value - DateTime.UnixEpoch;
        return Math.Max(0, (long)span.TotalMilliseconds);
    }

    public static DateTime FromUnixTimeMilliseconds(long value)
    {
        return DateTime.UnixEpoch.AddMilliseconds(value);
    }
    
    public static DateTime FromUnixTimeSeconds(long value)
    {
        return DateTime.UnixEpoch.AddSeconds(value);
    }
}