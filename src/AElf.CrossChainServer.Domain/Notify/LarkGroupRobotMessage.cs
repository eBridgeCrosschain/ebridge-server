#nullable enable
using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace AElf.CrossChainServer.Notify;

public enum LarkMessageTypeEnum
{
    [Description("text")] Text = 0,
    [Description("post")] Post = 1,
    [Description("share_chat")] ShareChat = 2,
    [Description("image")] Image = 3,
    [Description("interactive")] Interactive = 4,
    
}

public abstract class LarkGroupRobotMessageBase
{
    [JsonProperty("msg_type")] public string MessageType { get; set; }
    public string? Timestamp { get; set; }
    public string? Sign { get; set; }

    public LarkGroupRobotMessageBase(LarkMessageTypeEnum messageType)
    {
        MessageType = EnumHelper.GetDescription(messageType);
    }
}

public class LarkMessageBuilder
{
    public string? Timestamp { get; set; }
    public string? Sign { get; set; }

    /// <summary>
    ///     card message builder
    /// </summary>
    /// <returns></returns>
    public static CardMessageBuilder CardMessageBuilder()
    {
        return LarkCardMessage.Builder();
    }
    
    protected void WithSign(string secret)
    {
        if (secret.IsNullOrEmpty()) return;
        
        var ts = (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        Timestamp = ts.ToString();
        Sign = GenSign(secret, ts);
    }


    public T Build<T>(T message) where T : LarkGroupRobotMessageBase
    {
        message.Sign = Sign;
        message.Timestamp = Timestamp;
        return message;
    }
    
    private static string GenSign(string secret, int timestamp)
    {
        var stringToSign = timestamp + "\n" + secret;
        var encoding = new UTF8Encoding();
        using var hmacSha256 = new HMACSHA256(encoding.GetBytes(stringToSign));
        var hashMessage = hmacSha256.ComputeHash(Array.Empty<byte>());
        return Convert.ToBase64String(hashMessage);
    }

}