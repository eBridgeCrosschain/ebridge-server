using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos;
/*
 *event NewReceipt(
 *     string receiptId,
 *   address asset,
 *    address owner,
 *    uint256 amount,
 *    string targetChainId,
 *    bytes32 targetAddress,
 *    uint256 blockTime  
 *);
 */
[Event("NewReceipt")]
public class NewReceiptEvent: IEventDTO
{
    [Parameter("string", "receiptId", 1, false)]
    public string ReceiptId { get; set; }
    [Parameter("address", "asset", 2, false)]
    public string Asset { get; set; }
    [Parameter("address", "owner", 3, false)]
    public string Owner { get; set; }

    [Parameter("uint256", "amount", 4, false)]
    public BigInteger Amount { get; set; }
    
    [Parameter("string", "targetChainId", 5, false)]
    public string TargetChainId { get; set; }
    
    [Parameter("bytes32", "targetAddress", 6, false)]
    public byte[] TargetAddress { get; set; }
    [Parameter("uint256","blockTime",7,false)]
    public BigInteger BlockTime { get; set; }

}