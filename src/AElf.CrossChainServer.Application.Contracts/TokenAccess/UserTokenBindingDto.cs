using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenBindingDto
{
    [Required] public string BindingId { get; set; }
    [Required] public string ThirdTokenId { get; set; }
}