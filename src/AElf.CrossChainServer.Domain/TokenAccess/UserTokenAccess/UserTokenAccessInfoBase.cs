using System;
using AElf.CrossChainServer.Entities;
using JetBrains.Annotations;
using Nest;

namespace AElf.CrossChainServer.TokenAccess.UserTokenAccess;

public class UserTokenAccessInfoBase : CrossChainServerEntity<Guid>
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] [CanBeNull] public string OfficialWebsite { get; set; }
    [Keyword] [CanBeNull] public string OfficialTwitter { get; set; }
    [Keyword] [CanBeNull] public string Title { get; set; }
    [Keyword] [CanBeNull] public string PersonName { get; set; }
    [Keyword] [CanBeNull] public string TelegramHandler { get; set; }
    [Keyword] public string Email { get; set; }
}