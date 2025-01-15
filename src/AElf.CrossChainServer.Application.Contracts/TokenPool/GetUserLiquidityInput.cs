using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenPool;

public class GetUserLiquidityInput
{
    [Required] public List<string> Providers { get; set; }
    [CanBeNull] public string ChainId { get; set; }
    [CanBeNull] public string Symbol { get; set; } 
    [CanBeNull] public string TokenAddress { get; set; }

}