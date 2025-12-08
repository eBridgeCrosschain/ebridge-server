using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.CrossChain;

public class GetCrossChainTransferStatusInput
{
    [MaxLength(length:10)]
    public List<Guid> Ids { get; set; } = new();
}