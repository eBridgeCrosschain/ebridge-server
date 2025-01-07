using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenAccessInfoBaseInput
{
    [Required] public string Symbol { get; set; }
}

public class UserTokenAccessInfoInput : UserTokenAccessInfoBaseInput
{
    [CanBeNull] public string OfficialWebsite { get; set; }
    [CanBeNull] public string OfficialTwitter { get; set; }
    [CanBeNull] public string Title { get; set; }
    [CanBeNull] public string PersonName { get; set; }
    [CanBeNull] public string TelegramHandler { get; set; }
    [Required] public string Email { get; set; }
}