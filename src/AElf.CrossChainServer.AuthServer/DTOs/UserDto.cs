using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.Auth.DTOs;

public class UserDto : CrossChainServerEntity<Guid>
{
    public Guid UserId { get; set; }
    public string AppId { get; set; }
    public string CaHash { get; set; }
    public List<AddressInfoDto> AddressInfos { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
}

public class AddressInfoDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
}