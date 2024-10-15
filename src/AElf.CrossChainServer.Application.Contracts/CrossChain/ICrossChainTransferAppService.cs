using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainTransferAppService
{
    Task<PagedResultDto<CrossChainTransferIndexDto>> GetListAsync(GetCrossChainTransfersInput input);
    Task<ListResultDto<CrossChainTransferStatusDto>> GetStatusAsync(GetCrossChainTransferStatusInput input);
    Task TransferAsync(CrossChainTransferInput input);
    Task ReceiveAsync(CrossChainReceiveInput input);
    Task UpdateProgressAsync();
    Task AddIndexAsync(AddCrossChainTransferIndexInput input);
    Task UpdateIndexAsync(UpdateCrossChainTransferIndexInput input);
    Task UpdateReceiveTransactionAsync();
    Task AutoReceiveAsync();

    Task Test();
}