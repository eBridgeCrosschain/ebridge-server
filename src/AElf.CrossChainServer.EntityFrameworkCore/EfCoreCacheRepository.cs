using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AElf.CrossChainServer
{
    public class EfCoreCacheRepository<TDbContext, TEntity, TKey>
        : EfCoreRepository<TDbContext, TEntity, TKey>
        where TDbContext : IEfCoreDbContext
        where TEntity : class, IEntity<TKey>
    {
        private IDistributedCache<TEntity> Cache =>
            this.LazyServiceProvider.LazyGetService<IDistributedCache<TEntity>>();
        
        public EfCoreCacheRepository(IDbContextProvider<TDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public override async Task<TEntity> GetAsync(
            TKey id,
            bool includeDetails = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity = await Cache.GetAsync(id.ToString(), token: cancellationToken);
            if (entity == null)
            {
                entity = await base.GetAsync(id, includeDetails, cancellationToken);
                await Cache.SetAsync(id.ToString(), entity, token: cancellationToken);
            }

            return entity;
        }

        public override async Task<TEntity> FindAsync(
            TKey id,
            bool includeDetails = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity = await Cache.GetAsync(id.ToString(), token: cancellationToken);
            if (entity == null)
            {
                entity = await base.FindAsync(id, includeDetails, cancellationToken);
                await Cache.SetAsync(id.ToString(), entity, token: cancellationToken);
            }

            return entity;
        }

        public override async Task DeleteAsync(
            TKey id,
            bool autoSave = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.DeleteAsync(id, autoSave, cancellationToken);
            await Cache.RemoveAsync(id.ToString(), token: cancellationToken);
        }

        public override async Task<TEntity> InsertAsync(
            TEntity entity,
            bool autoSave = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var insertedEntity = await base.InsertAsync(entity, autoSave, cancellationToken);
            await Cache.SetAsync(insertedEntity.ToString(), insertedEntity, token: cancellationToken);
            return insertedEntity;
        }

        public override async Task<TEntity> UpdateAsync(
            TEntity entity,
            bool autoSave = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var updatedEntity = await base.UpdateAsync(entity, autoSave, cancellationToken);
            await Cache.SetAsync(updatedEntity.Id.ToString(), updatedEntity, token: cancellationToken);
            return updatedEntity;
        }
    }
}