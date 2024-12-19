using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.TokenPool;

public interface IUserLiquidityInfoAppService
{
    Task<List<UserLiquidityIndexDto>> GetUserLiquidityInfosAsync(GetUserLiquidityInput input);
    Task AddUserLiquidityAsync(UserLiquidityInfoInput input);
    Task RemoveUserLiquidityAsync(UserLiquidityInfoInput input);
    Task AddIndexAsync(AddUserLiquidityInfoIndexInput input);
    Task UpdateIndexAsync(UpdateUserLiquidityInfoIndexInput input);
}