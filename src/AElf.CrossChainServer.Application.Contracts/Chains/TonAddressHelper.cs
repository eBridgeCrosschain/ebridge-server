using TonSdk.Core;

namespace AElf.CrossChainServer.Chains;

public partial class TonAddressHelper
{
    private const bool IsTestOnly = true;
    private const bool IsBounceable = false;

    public static string GetTonUserFriendlyAddress(string rawAddress)
    {
        var splitRaw = rawAddress.Split(":");
        var wc = int.Parse(splitRaw[0]);
        var hash = ByteArrayHelper.HexStringToByteArray(splitRaw[1]);
        var address = new Address(wc, hash,new AddressRewriteOptions
        {
            Bounceable = IsBounceable,
            TestOnly = IsTestOnly
        });
        return address.ToString();
    }
    
    public static string GetTonRawAddress(string address)
    {
        var tonAddress = new Address(address);
        var hash = tonAddress.GetHash();
        var wc = tonAddress.GetWorkchain();
        var raw = wc + ":" + hash.ToHex();
        return raw; 
    }
    public static bool IsTonFriendlyAddress(string source)
    {
        // Check length
        if (source.Length != 48)
        {
            return false;
        }

        // Check if address is valid base64
        return Base64Regex().IsMatch(source);
    }
    
    public static bool IsTonRawAddress(string source)
    {
        // Check if has delimiter
        if (!source.Contains(':'))
        {
            return false;
        }

        var parts = source.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var wc = parts[0];
        var hash = parts[1];

        // Check if wc is valid
        if (!int.TryParse(wc, out _))
        {
            return false;
        }

        // Check if hash is valid
        if (!HashRegex().IsMatch(hash.ToLower()))
        {
            return false;
        }

        // Check if hash length is correct
        return hash.Length == 64;
    }
    
    public static string ConvertRawAddressToFriendly(string address)
    {
        return IsTonRawAddress(address) ? GetTonUserFriendlyAddress(address) : address;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Za-z0-9+/_-]+$")]
    private static partial System.Text.RegularExpressions.Regex Base64Regex();
    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-f0-9]+$")]
    private static partial System.Text.RegularExpressions.Regex HashRegex();
}

public class AddressRewriteOptions : IAddressRewriteOptions
{
    public int? Workchain { get; set; }
    public bool? Bounceable { get; set; }
    public bool? TestOnly { get; set; }
}