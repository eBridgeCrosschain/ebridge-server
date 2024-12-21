using System;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenAccessInfo : CrossChainServerEntity<Guid>
{
    public override Guid Id { get; set; }
    public string Symbol { get; set; }
    public string Address { get; set; }
    public string OfficialWebsite { get; set; }
    public string OfficialTwitter { get; set; }
    public string Title { get; set; }
    public string PersonName { get; set; }
    public string TelegramHandler { get; set; }
    public string Email { get; set; }
    public string ChainIds { get; set; } = "[]";
    public string OtherChainIds { get; set; } = "[]";
}