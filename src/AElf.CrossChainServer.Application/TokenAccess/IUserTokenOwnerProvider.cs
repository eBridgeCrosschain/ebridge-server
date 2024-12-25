using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess.UserTokenOwner;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.TokenAccess;

public interface IUserTokenOwnerProvider
{
    Task AddUserTokenOwnerListAsync(string address, List<UserTokenOwnerInfoDto> userTokenOwnerDtoList);
    Task<List<UserTokenOwnerInfoDto>> GetUserTokenOwnerListAsync(string address);
}

public class UserTokenOwnerProvider : IUserTokenOwnerProvider
{
    private readonly IDistributedCache<List<UserTokenOwnerInfo>> _userTokenOwnerCache;
    private readonly IObjectMapper _objectMapper;
    private const string CachePrefix = "EbridgeServer:OwnerTokenList:";

    public UserTokenOwnerProvider(IDistributedCache<List<UserTokenOwnerInfo>> userTokenOwnerCache,
        IObjectMapper objectMapper, IDistributedCache distributedCache)
    {
        _userTokenOwnerCache = userTokenOwnerCache;
        _objectMapper = objectMapper;
    }

    public async Task AddUserTokenOwnerListAsync(string address, List<UserTokenOwnerInfoDto> userTokenOwnerDtoList)
    {
        var result = new List<UserTokenOwnerInfo>();
        foreach (var info in userTokenOwnerDtoList.Select(userTokenOwnerInfoDto =>
                     _objectMapper.Map<UserTokenOwnerInfoDto, UserTokenOwnerInfo>(userTokenOwnerInfoDto)))
        {
            info.Id = Guid.NewGuid();
            result.Add(info);
        }

        await _userTokenOwnerCache.SetAsync(GetCacheKey(address), result, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddDays(7)
        });
    }

    public async Task<List<UserTokenOwnerInfoDto>> GetUserTokenOwnerListAsync(string address)
    {
        var key = GetCacheKey(address);
        var info = await _userTokenOwnerCache.GetAsync(key);
        return info == null
            ? new List<UserTokenOwnerInfoDto>()
            : _objectMapper.Map<List<UserTokenOwnerInfo>, List<UserTokenOwnerInfoDto>>(info);
    }

    private string GetCacheKey(string key)
    {
        return $"{CachePrefix}:{key}";
    }
}