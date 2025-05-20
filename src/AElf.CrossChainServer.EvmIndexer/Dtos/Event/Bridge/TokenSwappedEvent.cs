using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.EvmIndexer.Dtos.Event.Bridge;

[Event("TokenSwapEvent")]
/*
 * event TokenSwapEvent(
 *    address receiveAddress,
 *    address token,
 *    uint256 amount,
 *    string receiptId,
 *    string fromChainId,
 *    uint256 blockTime  
 * );
 */
public class TokenSwappedEvent : IEventDTO
{
    [Parameter("address", "receiveAddress", 1, false)]
    public string ReceiveAddress { get; set; }

    [Parameter("address", "token", 2, false)]
    public string Token { get; set; }
    
    [Parameter("uint256", "amount", 3, false)]
    public BigInteger Amount { get; set; }

    [Parameter("string", "receiptId", 4, false)]
    public string ReceiptId { get; set; }

    [Parameter("string", "fromChainId", 5, false)]
    public string FromChainId { get; set; }
    [Parameter("uint256","blockTime",6,false)]
    public BigInteger BlockTime { get; set; }
}