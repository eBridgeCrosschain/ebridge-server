using System;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenPool;

public class MockTokenLiquidityMonitorProvider : ITokenLiquidityMonitorProvider
{
    public Task MonitorTokenLiquidityAsync(string chainId, Guid tokenId, decimal poolLiquidity)
    {
        return Task.CompletedTask;
    }
}