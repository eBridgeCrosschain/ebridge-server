using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Token = AElf.CrossChainServer.Tokens.Token;

namespace AElf.CrossChainServer.TokenPool;

[RemoteService(IsEnabled = false)]
public class PoolLiquidityInfoAppService : CrossChainServerAppService, IPoolLiquidityInfoAppService
{
    private readonly IPoolLiquidityRepository _poolLiquidityRepository;
    private readonly INESTRepository<PoolLiquidityInfoIndex, Guid> _poolLiquidityInfoIndexRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly ITokenAppService _tokenAppService;
    private readonly IChainAppService _chainAppService;
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly ISettingManager _settingManager;
    private readonly IBridgeContractAppService _bridgeContractAppService;
    private readonly BridgeContractOptions _bridgeContractOptions;
    private readonly PoolLiquiditySyncOptions _poolLiquiditySyncOptions;
    private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;
    private readonly ITokenLiquidityMonitorProvider _tokenLiquidityMonitorProvider;
    private readonly TokenAccessOptions _tokenAccessOptions;

    public PoolLiquidityInfoAppService(IPoolLiquidityRepository poolLiquidityRepository,
        INESTRepository<PoolLiquidityInfoIndex, Guid> poolLiquidityInfoIndexRepository,
        ITokenRepository tokenRepository, IChainAppService chainAppService, IBlockchainAppService blockchainAppService,
        ISettingManager settingManager,
        IOptionsSnapshot<BridgeContractOptions> bridgeContractOptions,
        IOptionsSnapshot<PoolLiquiditySyncOptions> poolLiquiditySyncOptions, ITokenAppService tokenAppService,
        IBridgeContractAppService bridgeContractAppService, ITokenApplyOrderRepository tokenApplyOrderRepository,
        ITokenLiquidityMonitorProvider tokenLiquidityMonitorProvider,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions,
        IThirdUserTokenIssueRepository thirdUserTokenIssueRepository)
    {
        _poolLiquidityRepository = poolLiquidityRepository;
        _poolLiquidityInfoIndexRepository = poolLiquidityInfoIndexRepository;
        _tokenRepository = tokenRepository;
        _chainAppService = chainAppService;
        _blockchainAppService = blockchainAppService;
        _settingManager = settingManager;
        _tokenAppService = tokenAppService;
        _bridgeContractAppService = bridgeContractAppService;
        _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _tokenLiquidityMonitorProvider = tokenLiquidityMonitorProvider;
        _thirdUserTokenIssueRepository = thirdUserTokenIssueRepository;
        _tokenAccessOptions = tokenAccessOptions.Value;
        _bridgeContractOptions = bridgeContractOptions.Value;
        _poolLiquiditySyncOptions = poolLiquiditySyncOptions.Value;
    }

    public async Task<PagedResultDto<PoolLiquidityIndexDto>> GetPoolLiquidityInfosAsync(
        GetPoolLiquidityInfosInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PoolLiquidityInfoIndex>, QueryContainer>>();
        if (input.ChainId == null)
        {
            var chainList = await _chainAppService.GetListAsync(new GetChainsInput());
            var chainIds = chainList.Items.Select(c => c.Id).ToList();
            if (chainIds.Any())
            {
                mustQuery.Add(q => q.Terms(t => t.Field(f => f.ChainId).Terms(chainIds)));
            }
        }
        else if (!string.IsNullOrWhiteSpace(input.ChainId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            if (!string.IsNullOrWhiteSpace(input.Token))
            {
                var chain = await _chainAppService.GetAsync(input.ChainId);
                switch (chain.Type)
                {
                    case BlockchainType.AElf:
                        mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenInfo.Symbol).Value(input.Token)));
                        break;
                    case BlockchainType.Evm:
                        mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenInfo.Address).Value(input.Token)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        QueryContainer Filter(QueryContainerDescriptor<PoolLiquidityInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _poolLiquidityInfoIndexRepository.GetListAsync(Filter, limit: input.MaxResultCount,
            skip: input.SkipCount, sortExp: o => o.Liquidity,
            sortType: SortOrder.Descending);
        var totalCount = await _poolLiquidityInfoIndexRepository.CountAsync(Filter);

        return new PagedResultDto<PoolLiquidityIndexDto>
        {
            TotalCount = totalCount.Count,
            Items = ObjectMapper.Map<List<PoolLiquidityInfoIndex>, List<PoolLiquidityIndexDto>>(list.Item2)
        };
    }

    public async Task AddLiquidityAsync(PoolLiquidityInfoInput input)
    {
        var liquidityInfo = await FindPoolLiquidityInfoAsync(input.ChainId, input.TokenId);
        var isLiquidityExist = true;
        if (liquidityInfo == null)
        {
            Log.Debug("New pool liquidity info. {chainId}-{tokenId}-{amount}", input.ChainId, input.TokenId,
                input.Liquidity);
            isLiquidityExist = false;
            liquidityInfo = ObjectMapper.Map<PoolLiquidityInfoInput, PoolLiquidityInfo>(input);
            await DealUpdateOrderInfoAsync(input.ChainId, input.TokenId,input.Provider);
        }
        else
        {
            Log.Debug("Update pool liquidity info.{chainId}-{tokenId}-{amount}", input.ChainId, input.TokenId,
                input.Liquidity);
            liquidityInfo.Liquidity += input.Liquidity;
        }

        if (isLiquidityExist)
        {
            await _poolLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }
        else
        {
            await _poolLiquidityRepository.InsertAsync(liquidityInfo, autoSave: true);
        }

        await _tokenLiquidityMonitorProvider.MonitorTokenLiquidityAsync(liquidityInfo.ChainId, liquidityInfo.TokenId,
            liquidityInfo.Liquidity);
    }

    public async Task RemoveLiquidityAsync(PoolLiquidityInfoInput input)
    {
        var liquidityInfo = await FindPoolLiquidityInfoAsync(input.ChainId, input.TokenId);
        if (liquidityInfo == null)
        {
            Log.Error("Pool liquidity info not found. {chainId}-{tokenId}", input.ChainId, input.TokenId);
        }
        else
        {
            Log.Debug("Remove pool liquidity info.{chainId}-{tokenId}-{amount}", input.ChainId, input.TokenId,
                input.Liquidity);
            liquidityInfo.Liquidity -= input.Liquidity;
            await _poolLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }

        await _tokenLiquidityMonitorProvider.MonitorTokenLiquidityAsync(liquidityInfo.ChainId, liquidityInfo.TokenId,
            liquidityInfo.Liquidity);
    }

    public async Task AddIndexAsync(AddPoolLiquidityInfoIndexInput input)
    {
        var index = ObjectMapper.Map<AddPoolLiquidityInfoIndexInput, PoolLiquidityInfoIndex>(input);

        if (input.TokenId != Guid.Empty)
        {
            index.TokenInfo = await _tokenRepository.GetAsync(input.TokenId);
        }

        await _poolLiquidityInfoIndexRepository.AddAsync(index);
    }

    public async Task UpdateIndexAsync(UpdatePoolLiquidityInfoIndexInput input)
    {
        var index = ObjectMapper.Map<UpdatePoolLiquidityInfoIndexInput, PoolLiquidityInfoIndex>(input);

        if (input.TokenId != Guid.Empty)
        {
            index.TokenInfo = await _tokenRepository.GetAsync(input.TokenId);
        }

        await _poolLiquidityInfoIndexRepository.UpdateAsync(index);
    }

    public async Task SyncPoolLiquidityInfoFromChainAsync()
    {
        Log.Information("Start to sync pool liquidity info from chain.");
        var chainList = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.AElf
        });
        foreach (var chain in chainList.Items)
        {
            Log.Debug("Sync pool liquidity info from chain {chainId}.", chain.Id);
            // // Step 1: Retrieve the current height of the best chain on AElf and insert a new AElf liquidity sync height
            var chainStatus = await _blockchainAppService.GetChainStatusAsync(chain.Id);
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.PoolLiquidityIndexerSync, null),
                chainStatus.BlockHeight.ToString());
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.UserLiquidityIndexerSync, null),
                chainStatus.BlockHeight.ToString());
            // Step 2: Query the AElf contract to sync the liquidity of configured tokens - getLiquidityInfo. 
            var tokenSymbols = _poolLiquiditySyncOptions.Token[chain.Id];
            var tokenIdList = new List<Guid>();
            foreach (var symbol in tokenSymbols)
            {
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = chain.Id,
                    Symbol = symbol
                });
                tokenIdList.Add(token.Id);
            }

            var poolLiquidityList = await _bridgeContractAppService.GetPoolLiquidityAsync(chain.Id,
                _bridgeContractOptions.ContractAddresses[chain.Id].TokenPoolContract, tokenIdList);
            // Step 3: Write the data into poolLiquidity.  
            foreach (var poolLiquidity in poolLiquidityList)
            {
                await AddLiquidityAsync(ObjectMapper.Map<PoolLiquidityDto, PoolLiquidityInfoInput>(poolLiquidity));
            }
        }

        var evmChainList = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Evm
        });
        foreach (var chain in evmChainList.Items)
        {
            Log.Debug("Sync pool liquidity info from chain {chainId}.", chain.Id);
            // Step 1: Retrieve the current height of the EVM chain and insert a new EVM liquidity sync height.  
            var currentChainHeight = await _blockchainAppService.GetChainHeightAsync(chain.Id);
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.EvmPoolLiquidityIndexerSync, null),
                currentChainHeight.ToString());
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.EvmUserLiquidityIndexerSync, null),
                currentChainHeight.ToString());
            // Step 2: Query the EVM contract to sync the liquidity of configured tokens - getBalance.  
            var tokenAddresses = _poolLiquiditySyncOptions.Token[chain.Id];
            var tokenIdList = new List<Guid>();
            foreach (var tokenAddress in tokenAddresses)
            {
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = chain.Id,
                    Address = tokenAddress
                });
                tokenIdList.Add(token.Id);
            }

            var poolLiquidityList = await _bridgeContractAppService.GetPoolLiquidityAsync(chain.Id,
                _bridgeContractOptions.ContractAddresses[chain.Id].TokenPoolContract, tokenIdList);
            // Step 3: Write the data into poolLiquidity.  
            foreach (var poolLiquidity in poolLiquidityList)
            {
                await AddLiquidityAsync(ObjectMapper.Map<PoolLiquidityDto, PoolLiquidityInfoInput>(poolLiquidity));
            }
        }

        Log.Information("Finish to sync pool liquidity info from chain.");
    }

    private async Task<PoolLiquidityInfo> FindPoolLiquidityInfoAsync(string chainId, Guid tokenId)
    {
        return await _poolLiquidityRepository.FindAsync(o => o.ChainId == chainId && o.TokenId == tokenId);
    }

    private string GetSettingKey(string syncType, string typePrefix)
    {
        return string.IsNullOrWhiteSpace(typePrefix) ? syncType : $"{typePrefix}-{syncType}";
    }

    private async Task DealUpdateOrderInfoAsync(string chainId, Guid tokenId,string provider)
    {
        var chain = await _chainAppService.GetAsync(chainId);
        if (chain.Type == BlockchainType.AElf || !string.Equals(provider,CrossChainServerConsts.AddressZero))
        {
            Log.Information("Not deal with aelf chain liquidity or provider is not zero address.{chainId},{provider}",
                chainId, provider);
            return;
        }
        var token = await _tokenRepository.GetAsync(tokenId);
        var queryable = await _tokenApplyOrderRepository.WithDetailsAsync(y => y.StatusChangedRecords);
        var query = queryable.Where(x => x.Symbol == token.Symbol && x.ChainId == chainId);
        var order = await AsyncExecuter.FirstOrDefaultAsync(query);
        if (order == null)
        {
            Log.Information(
                "Token apply order not found,chainId: {chainId},symbol: {symbol}", chainId, token.Symbol);
            var thirdTokenInfo =
                await _thirdUserTokenIssueRepository.FindAsync(t => t.Symbol == token.Symbol && t.ChainId == chainId);
            if (thirdTokenInfo == null)
            {
                Log.Warning("Third token info not found,chainId: {chainId},symbol: {symbol}", chainId, token.Symbol);
                return;
            }
            await CreateTokenApplyOrderAsync(token.Symbol, thirdTokenInfo.Address, TokenApplyOrderStatus.PoolInitialized.ToString(),
                thirdTokenInfo);
        }
        else
        {
            if (order.Status != TokenApplyOrderStatus.PoolInitializing.ToString())
            {
                Log.Warning("Invalid token apply order status, orderId: {orderId}", order.Id);
                return;
            }

            order.Status = TokenApplyOrderStatus.PoolInitialized.ToString();
            order.StatusChangedRecords.Add(new StatusChangedRecord
            {
                Status = TokenApplyOrderStatus.PoolInitialized.ToString(),
                Time = DateTime.UtcNow
            });
            await _tokenApplyOrderRepository.UpdateAsync(order, autoSave: true);
        }
        await DealWithAElfChainLiquidityAsync(token);
    }

    private async Task DealWithAElfChainLiquidityAsync(Token token)
    {
        var poolList = await _poolLiquidityRepository.GetListAsync(p => p.TokenId == token.Id);
        var chainList = poolList.Select(p => p.ChainId).ToHashSet();
        var chainsToInsert = new List<PoolLiquidityInfo>();

        if (token.IsBurnable)
        {
            chainsToInsert.AddRange(_tokenAccessOptions.ChainIdList
                .Where(aelfChainId => !chainList.Contains(aelfChainId)).Select(aelfChainId =>
                    new PoolLiquidityInfo { ChainId = aelfChainId, TokenId = token.Id, Liquidity = 0 }));
        }
        else
        {
            var aelfChain = await _chainAppService.GetByAElfChainIdAsync(token.IssueChainId);
            if (!chainList.Contains(aelfChain.Id))
            {
                chainsToInsert.Add(new PoolLiquidityInfo
                {
                    ChainId = aelfChain.Id,
                    TokenId = token.Id,
                    Liquidity = 0
                });
            }
        }

        if (chainsToInsert.Count != 0)
        {
            await _poolLiquidityRepository.InsertManyAsync(chainsToInsert, autoSave: true);
        }
    }
    
    private async Task CreateTokenApplyOrderAsync(string symbol, string userAddress, string status,
        ThirdUserTokenIssueInfo tokenIssueInfo)
    {
        var order = new TokenApplyOrder
        {
            Symbol = symbol,
            UserAddress = userAddress,
            Status = status,
            CreateTime = ToUtcMilliSeconds(DateTime.UtcNow),
            UpdateTime = ToUtcMilliSeconds(DateTime.UtcNow),
            StatusChangedRecords = new List<StatusChangedRecord>
            {
                new() { Id = Guid.NewGuid(), Status = status, Time = DateTime.UtcNow }
            },
            ChainId = tokenIssueInfo.OtherChainId,
            ChainName = tokenIssueInfo.OtherChainId,
            TokenName = tokenIssueInfo.TokenName,
            TotalSupply = tokenIssueInfo.TotalSupply.SafeToDecimal(),
            Decimals = CrossChainServerConsts.DefaultEvmTokenDecimal,
            Icon = tokenIssueInfo.TokenImage,
            PoolAddress = _bridgeContractOptions.ContractAddresses[tokenIssueInfo.OtherChainId].TokenPoolContract,
            ContractAddress = tokenIssueInfo.ContractAddress
        };
        await _tokenApplyOrderRepository.InsertAsync(order);
    }
    
    public static long ToUtcMilliSeconds(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }
}