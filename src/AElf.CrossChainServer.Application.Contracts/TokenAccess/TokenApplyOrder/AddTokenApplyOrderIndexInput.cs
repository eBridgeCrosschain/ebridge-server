using System;
using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class AddTokenApplyOrderIndexInput
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string UserAddress { get; set; }
    public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public List<ChainTokenInfoDto> ChainTokenInfo { get; set; }
    public List<StatusChangedRecordDto> StatusChangedRecords { get; set; }
}