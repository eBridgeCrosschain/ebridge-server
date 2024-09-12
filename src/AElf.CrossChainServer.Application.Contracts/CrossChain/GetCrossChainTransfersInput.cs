using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.CrossChain;

public class GetCrossChainTransfersInput : PagedAndSortedResultRequestDto
{
    [MaxLength(length:20)]
    public string FromChainId { get; set; }
    [MaxLength(length:20)]
    public string ToChainId { get; set; }
    [MaxLength(length:80)]
    public string FromAddress { get; set; }
    [MaxLength(length:80)]
    public string ToAddress { get; set; }
    public CrossChainStatus? Status { get; set; }
    public CrossChainType? Type { get; set; }

    public string Addresses { get; set; }
}