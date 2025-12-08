using System;
using AElf.AElfNode.EventHandler.BackgroundJob;

namespace AElf.CrossChainServer.ContractEventHandler
{
    public class EventContextHelper
    {
        public static EventContext Create(string methodName, int chainId =9992731)
        {
            var txId = "20ab8470f71572fa553b6fd7cd8274c382d0d13695b9444ac65321955bc638ff";
            var returnValue = string.Empty;
            var blockNumber = 1000;
            var blockTime = DateTime.UtcNow;
            var fromAddress = "0xUser";
            var toAddress = "0xFactoryA";
            var blockHash = "0x0834ca06d6211906c1a2eb64a04fc1";
            
            return new EventContext
            {
                TransactionId = txId,
                Status = "MINED",
                ReturnValue = returnValue,
                ChainId = chainId,
                BlockNumber = blockNumber,
                MethodName = methodName,
                BlockTime = blockTime,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                BlockHash = blockHash
            };
        }
    }
}