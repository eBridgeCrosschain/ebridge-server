using System;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.Tokens
{
    public class TokenDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
        public int IssueChainId { get; set; }
        public string Owner { get; set; }
        public string Icon { get; set; }

    }
}