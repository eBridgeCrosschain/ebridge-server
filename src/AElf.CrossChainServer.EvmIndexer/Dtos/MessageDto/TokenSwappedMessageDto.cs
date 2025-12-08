using System.Numerics;

namespace AElf.CrossChainServer.EvmIndexer.Dtos.MessageDto;

public class TokenSwappedMessageDto
{
    public string TransactionId { get; set; }
    public long BlockNumber { get; set; }
    public string ReceiveAddress { get; set; }
    
    public string Token { get; set; }
    
    public BigInteger Amount { get; set; }

    public string ReceiptId { get; set; }

    public string FromChainId { get; set; }

    public BigInteger BlockTime { get; set; }
}