using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenAccessInfoBase : CrossChainServerEntity<Guid>
{
    [Keyword]
    public string Symbol { get; set; }
    [Keyword]
    public string Address { get; set; }
    [Keyword]
    public string OfficialWebsite { get; set; }
    [Keyword]
    public string OfficialTwitter { get; set; }
    [Keyword]
    public string Title { get; set; }
    [Keyword]
    public string PersonName { get; set; }
    [Keyword]
    public string TelegramHandler { get; set; }
    [Keyword]
    public string Email { get; set; }
}