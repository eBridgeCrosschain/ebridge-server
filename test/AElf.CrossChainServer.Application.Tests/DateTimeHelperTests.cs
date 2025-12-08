using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer;

public class DateTimeHelperTests
{
    [Fact]
    public async Task Test()
    {
        var date = DateTime.MinValue;
        var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
        timestamp.ShouldBe(0);
        
        timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
        var newDate = DateTimeHelper.FromUnixTimeMilliseconds(timestamp);
        var newTimestamp = DateTimeHelper.ToUnixTimeMilliseconds(newDate);
        newTimestamp.ShouldBe(timestamp);
    }
}