using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using AElf.CrossChainServer.TokenAccess.UserTokenAccess;
using AElf.CrossChainServer.TokenPool;
using AutoMapper;

namespace AElf.CrossChainServer
{
    public class DomainAutoMapperProfile : Profile
    {
        public DomainAutoMapperProfile()
        {
            CreateMap<CrossChainTransfer, CrossChainTransferEto>();
            CreateMap<CrossChainIndexingInfo, CrossChainIndexingInfoEto>();
            CreateMap<CrossChainDailyLimit, CrossChainDailyLimitEto>();
            CreateMap<CrossChainRateLimit, CrossChainRateLimitEto>();
            CreateMap<PoolLiquidityInfo, PoolLiquidityEto>();
            CreateMap<UserLiquidityInfo, UserLiquidityEto>();
            CreateMap<UserTokenAccessInfo, UserTokenAccessInfoEto>();
            CreateMap<TokenApplyOrder, TokenApplyOrderEto>();
            CreateMap<StatusChangedRecord, StatusChangedRecordDto>();
            CreateMap<ThirdUserTokenIssueInfo, ThirdUserTokenIssueInfoEto>();

        }
    }
}