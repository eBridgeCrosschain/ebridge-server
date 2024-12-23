using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenAccessInfoBaseInput
{
    [Required] public string Symbol { get; set; }
}

public class UserTokenAccessInfoInput : UserTokenAccessInfoBaseInput
{
    [Required] public string OfficialWebsite { get; set; }
    [Required] public string OfficialTwitter { get; set; }
    [Required] public string Title { get; set; }
    [Required] public string PersonName { get; set; }
    [Required] public string TelegramHandler { get; set; }
    [Required] public string Email { get; set; }
}