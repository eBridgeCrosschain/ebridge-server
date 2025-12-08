using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.Filter;

public class ValidationResponseDto : ResponseDto
{
    public IList<ValidationResult> ValidationErrors { get; set; }
}