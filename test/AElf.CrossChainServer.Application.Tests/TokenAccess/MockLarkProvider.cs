using System.Threading.Tasks;
using AElf.CrossChainServer.Notify;

namespace AElf.CrossChainServer.TokenAccess;

public class MockLarkProvider : ILarkRobotNotifyProvider
{
    public async Task<bool> SendMessageAsync(NotifyRequest notifyRequest)
    {
        return true;
    }
}