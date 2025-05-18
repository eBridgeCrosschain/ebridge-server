using System.Numerics;

namespace AElf.CrossChainServer.EvmIndexer.Dtos.MessageDto;

public class NewReceiptReceivedMessageDto
{
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string ReceiptId { get; set; }
    
    public string Asset { get; set; }

    public string Owner { get; set; }


    public BigInteger Amount { get; set; }
    

    public string TargetChainId { get; set; }

    public byte[] TargetAddress { get; set; }
    public BigInteger BlockTime { get; set; }
}