using System;
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
    Task DeleteIndexAsync(Guid id);
    Task UpdateReceiveTransactionAsync();
    Task AutoReceiveAsync();
    Task CheckReceiveTransactionAsync();
    Task CheckTransferTransactionConfirmedAsync(string chainId);
    Task CheckReceiveTransactionConfirmedAsync(string chainId);
    Task CheckEvmTransferTransactionConfirmedAsync(string chainId);
    Task CheckEvmReceiveTransactionConfirmedAsync(string chainId);
}