using System;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.Settings;

public class Settings : MultiChainEntity<Guid>
{
    public string Name { get; set; }
    public string Value { get; set; }
}