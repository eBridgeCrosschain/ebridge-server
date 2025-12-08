using System.Security.Cryptography;
using System.Text;

namespace AElf.CrossChainServer.Auth.Common;

public static class GuidHelper
{
    public static Guid UniqGuid(params string[] paramArr)
        => new(MD5.HashData(Encoding.Default.GetBytes(GenerateId(paramArr))));

    public static string GenerateId(params string[] paramArr) => string.Join("_", paramArr);
    public static string GenerateGrainId(params object[] ids) => ids.JoinAsString("-");
    public static string GenerateCombinedId(params string[] parts) => string.Join(CommonConstant.Colon, parts);
}