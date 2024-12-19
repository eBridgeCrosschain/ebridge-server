namespace AElf.CrossChainServer.TokenAccess;

public enum TokenApplyOrderStatus
{
    Unissued,
    Issuing,
    Issued,
    Reviewing,
    Rejected,
    Reviewed,
    PoolInitializing,
    PoolInitialized,
    Integrating,
    Complete,
    Failed
}