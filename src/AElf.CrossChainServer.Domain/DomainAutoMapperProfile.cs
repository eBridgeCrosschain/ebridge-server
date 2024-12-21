using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenAccess;
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
            CreateMap<OracleQueryInfo, OracleQueryInfoEto>();
            CreateMap<ReportInfo, ReportInfoEto>();
            CreateMap<PoolLiquidityInfo, PoolLiquidityEto>();
            CreateMap<UserLiquidityInfo, UserLiquidityEto>();
            CreateMap<UserTokenAccessInfo, UserTokenAccessInfoEto>();
        }
    }
}