﻿using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using AutoMapper;

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
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.ProgressUpdateTime)));
        
        CreateMap<CrossChainTransferIndex, CrossChainTransferStatusDto>();
        CreateMap<CrossChainTransferEto, AddCrossChainTransferIndexInput>();
        CreateMap<CrossChainTransferEto, UpdateCrossChainTransferIndexInput>();

        CreateMap<BridgeContractSyncInfo, BridgeContractSyncInfoDto>();
    }
}
