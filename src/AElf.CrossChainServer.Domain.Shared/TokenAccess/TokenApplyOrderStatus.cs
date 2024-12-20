namespace AElf.CrossChainServer.TokenAccess;

public enum TokenApplyOrderStatus
{
    Unissued,
    Issuing,
    Issued,
    PoolInitializing,
    PoolInitialized,
    LiquidityAdded,
    Complete,
    Failed
}