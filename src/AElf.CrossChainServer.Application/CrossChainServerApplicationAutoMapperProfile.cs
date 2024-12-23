using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Chains.Ton;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using AutoMapper;
using TonAddressHelper = AElf.CrossChainServer.Chains.TonAddressHelper;
using Token = AElf.CrossChainServer.Tokens.Token;

namespace AElf.CrossChainServer;

public class CrossChainServerApplicationAutoMapperProfile : Profile
{
    public CrossChainServerApplicationAutoMapperProfile()
    {
        CreateMap<Chain, ChainDto>();
        CreateMap<Chain, ChainIndex>();
        CreateMap<ChainIndex, ChainDto>();
        CreateMap<CreateChainInput, Chain>();

        CreateMap<Token, TokenDto>();
        CreateMap<TokenCreateInput, Token>();

        CreateMap<CreateCrossChainIndexingInfoInput, CrossChainIndexingInfo>();
        CreateMap<AddCrossChainIndexingInfoIndexInput, CrossChainIndexingInfoIndex>();
        CreateMap<CrossChainIndexingInfoEto, AddCrossChainIndexingInfoIndexInput>();

        CreateMap<CreateOracleQueryInfoInput, OracleQueryInfo>();
        CreateMap<AddOracleQueryInfoIndexInput, OracleQueryInfoIndex>();
        CreateMap<UpdateOracleQueryInfoIndexInput, OracleQueryInfoIndex>();
        CreateMap<OracleQueryInfoEto, AddOracleQueryInfoIndexInput>();
        CreateMap<OracleQueryInfoEto, UpdateOracleQueryInfoIndexInput>();
        
        CreateMap<CreateReportInfoInput, ReportInfo>();
        CreateMap<AddReportInfoIndexInput, ReportInfoIndex>();
        CreateMap<UpdateReportInfoIndexInput, ReportInfoIndex>();
        CreateMap<ReportInfoEto, AddReportInfoIndexInput>();
        CreateMap<ReportInfoEto, UpdateReportInfoIndexInput>();

        CreateMap<CrossChainTransferInput, CrossChainTransfer>();
        CreateMap<CrossChainReceiveInput, CrossChainTransfer>();
        
        CreateMap<AddCrossChainTransferIndexInput, CrossChainTransferIndex>();
        CreateMap<UpdateCrossChainTransferIndexInput, CrossChainTransferIndex>();
        CreateMap<CrossChainTransferIndex, CrossChainTransferIndexDto>()
            .ForMember(destination => destination.TransferTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.TransferTime)))
            .ForMember(destination => destination.ReceiveTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.ReceiveTime)))
            .ForMember(destination => destination.ProgressUpdateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.ProgressUpdateTime)))
            .ForMember(destination => destination.FromAddress, opt => opt.MapFrom(source =>
                TonAddressHelper.ConvertRawAddressToFriendly(source.FromAddress)))
            .ForMember(destination => destination.ToAddress, opt => opt.MapFrom(source =>
                TonAddressHelper.ConvertRawAddressToFriendly(source.ToAddress)));
        CreateMap<CrossChainTransferIndex, CrossChainTransferStatusDto>();
        CreateMap<CrossChainTransferEto, AddCrossChainTransferIndexInput>();
        CreateMap<CrossChainTransferEto, UpdateCrossChainTransferIndexInput>();

        CreateMap<BridgeContractSyncInfo, BridgeContractSyncInfoDto>();

        CreateMap<TonIndexTransaction, TonTransactionDto>();
        CreateMap<TonBlockId, TonBlockIdDto>();
        CreateMap<TonMessage, TonMessageDto>();
        CreateMap<TonMessageContent, TonMessageContentDto>();
        CreateMap<TonDecodedContent, TonDecodedContentDto>();

        CreateMap<TonApiTransaction, TonApiTransactionDto>();
        CreateMap<TonApiAccount, TonApiAccountDto>();
        CreateMap<TonapiMessage, TonapiMessageDto>();
        CreateMap<TonapiComputePhase, TonapiComputePhaseDto>();
        CreateMap<TonapiStoragePhase, TonapiStoragePhaseDto>();
        CreateMap<TonapiCreditPhase, TonapiCreditPhaseDto>();
        CreateMap<TonapiActionPhase, TonapiActionPhaseDto>();

        CreateMap<SetCrossChainDailyLimitInput, CrossChainDailyLimit>();
        CreateMap<SetCrossChainRateLimitInput, CrossChainRateLimit>();
        CreateMap<SetCrossChainDailyLimitInput, CrossChainDailyLimitIndex>();
        CreateMap<SetCrossChainRateLimitInput, CrossChainRateLimitIndex>();
        CreateMap<CrossChainRateLimitEto, SetCrossChainRateLimitInput>();
        CreateMap<CrossChainDailyLimitEto, SetCrossChainDailyLimitInput>();
        CreateMap<CrossChainRateLimitIndex, CrossChainRateLimitDto>();

        CreateMap<UserTokenAccessInfoEto, AddUserTokenAccessInfoIndexInput>();
        CreateMap<UserTokenAccessInfoEto, UpdateUserTokenAccessInfoIndexInput>();
        CreateMap<AddUserTokenAccessInfoIndexInput, UserTokenAccessInfoIndex>();
        CreateMap<UpdateUserTokenAccessInfoIndexInput, UserTokenAccessInfoIndex>();
        CreateMap<TokenApplyOrderEto, AddTokenApplyOrderIndexInput>();
        CreateMap<TokenApplyOrderEto, UpdateTokenApplyOrderIndexInput>();
        CreateMap<AddTokenApplyOrderIndexInput, TokenApplyOrderIndex>();
        CreateMap<UpdateTokenApplyOrderIndexInput, TokenApplyOrderIndex>();

        CreateMap<PoolLiquidityInfoInput, PoolLiquidityInfo>();
        CreateMap<AddPoolLiquidityInfoIndexInput, PoolLiquidityInfoIndex>();
        CreateMap<UserLiquidityInfoInput, UserLiquidityInfo>();
        CreateMap<AddUserLiquidityInfoIndexInput, UserLiquidityInfoIndex>();
        CreateMap<PoolLiquidityEto, AddPoolLiquidityInfoIndexInput>();
        CreateMap<PoolLiquidityEto, UpdatePoolLiquidityInfoIndexInput>();
        CreateMap<UserLiquidityEto, AddUserLiquidityInfoIndexInput>();
        CreateMap<UserLiquidityEto, UpdateUserLiquidityInfoIndexInput>();
        CreateMap<PoolLiquidityInfoIndex, PoolLiquidityIndexDto>();
        CreateMap<UserLiquidityInfoIndex, UserLiquidityIndexDto>();

        CreateMap<UserTokenAccessInfoInput, UserTokenAccessInfo>().ReverseMap();
        CreateMap<UserTokenAccessInfoIndex, UserTokenAccessInfoDto>();
        CreateMap<UserTokenAccessInfoIndex, UserTokenAccessInfo>();
    }
}
