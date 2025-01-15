using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenAccess;

public class GetAvailableTokensInput
{
    [CanBeNull] public string Symbol { get; set; }
}