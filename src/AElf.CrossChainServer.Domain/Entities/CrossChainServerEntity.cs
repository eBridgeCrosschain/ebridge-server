using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace AElf.CrossChainServer.Entities;

/// <inheritdoc/>
[Serializable]
public abstract class CrossChainServerEntity : IEntity
{
    protected CrossChainServerEntity()
    {
        EntityHelper.TrySetTenantId(this);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Keys = {GetKeys().JoinAsString(", ")}";
    }

    public abstract object[] GetKeys();

    public bool EntityEquals(IEntity other)
    {
        return EntityHelper.EntityEquals(this, other);
    }
}

/// <inheritdoc cref="IEntity{TKey}" />
[Serializable]
public abstract class CrossChainServerEntity<TKey> : CrossChainServerEntity, IEntity<TKey>
{
    /// <inheritdoc/>
    public virtual TKey Id { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}
