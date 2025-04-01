using System;
using System.Security.Cryptography;
using System.Text;

namespace AElf.CrossChainServer;

public static class GuidHelper
{
    public static Guid UniqGuid(params string[] paramArr)
    {
        return new Guid(MD5.HashData(Encoding.Default.GetBytes(GenerateId(paramArr))));
    }
    
    public static string GenerateId(params string[] paramArr)
    {
        return string.Join("_", paramArr);
    }
}