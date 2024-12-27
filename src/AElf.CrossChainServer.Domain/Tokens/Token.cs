using System;
using AElf.CrossChainServer.Entities;
using JetBrains.Annotations;
using Nest;

namespace AElf.CrossChainServer.Tokens
{
    public class Token : MultiChainEntity<Guid>
    {
        [NotNull] [Keyword] public virtual string Address { get; set; }

        [NotNull] [Keyword] public virtual string Symbol { get; set; }

        public virtual int Decimals { get; set; }
        public virtual int IssueChainId { get; set; }

    }
}