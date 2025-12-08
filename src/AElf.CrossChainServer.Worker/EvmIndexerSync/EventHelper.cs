using Nethereum.Util;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public static class EventHelper
{
    public static string GetEventSignature(this string eventSig)
    {
        return new Sha3Keccack().CalculateHash(eventSig).ToString();
    }
    
}