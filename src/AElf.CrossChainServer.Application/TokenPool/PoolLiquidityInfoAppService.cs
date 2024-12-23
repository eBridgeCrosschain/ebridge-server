using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

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

    public PoolLiquidityInfoAppService(IPoolLiquidityRepository poolLiquidityRepository,
        INESTRepository<PoolLiquidityInfoIndex, Guid> poolLiquidityInfoIndexRepository,
        ITokenRepository tokenRepository, IChainAppService chainAppService, IBlockchainAppService blockchainAppService,
        ISettingManager settingManager,
        IOptionsSnapshot<BridgeContractOptions> bridgeContractOptions,
        IOptionsSnapshot<PoolLiquiditySyncOptions> poolLiquiditySyncOptions, ITokenAppService tokenAppService,
        IBridgeContractAppService bridgeContractAppService)
    {
        _poolLiquidityRepository = poolLiquidityRepository;
        _poolLiquidityInfoIndexRepository = poolLiquidityInfoIndexRepository;
        _tokenRepository = tokenRepository;
        _chainAppService = chainAppService;
        _blockchainAppService = blockchainAppService;
        _settingManager = settingManager;
        _tokenAppService = tokenAppService;
        _bridgeContractAppService = bridgeContractAppService;
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
            foreach (var chain in chainList.Items)
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chain.Id)));
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
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Debug("New pool liquidity info");
            isLiquidityExist = false;
            liquidityInfo = ObjectMapper.Map<PoolLiquidityInfoInput, PoolLiquidityInfo>(input);
        }
        else
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Debug("Update pool liquidity info");
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
    }

    public async Task RemoveLiquidityAsync(PoolLiquidityInfoInput input)
    {
        var liquidityInfo = await FindPoolLiquidityInfoAsync(input.ChainId, input.TokenId);
        if (liquidityInfo == null)
        {
            Log.ForContext("ChainId", input.ChainId)
                .ForContext("TokenId", input.TokenId)
                .Error("Pool liquidity info not found");
        }
        else
        {
            liquidityInfo.Liquidity -= input.Liquidity;
            await _poolLiquidityRepository.UpdateAsync(liquidityInfo, autoSave: true);
        }
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
            var bestChainHeight = await _blockchainAppService.GetChainStatusAsync(chain.Id);
            await _settingManager.SetAsync(chain.Id, CrossChainServerSettings.PoolLiquidityIndexerSync,
                bestChainHeight.BlockHeight.ToString());
            await _settingManager.SetAsync(chain.Id, CrossChainServerSettings.UserLiquidityIndexerSync,
                bestChainHeight.BlockHeight.ToString());
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
            await _settingManager.SetAsync(chain.Id, CrossChainServerSettings.EvmPoolLiquidityIndexerSync,
                currentChainHeight.ToString());
            await _settingManager.SetAsync(chain.Id, CrossChainServerSettings.EvmUserLiquidityIndexerSync,
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
}