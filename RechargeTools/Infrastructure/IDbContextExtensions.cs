using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Entity;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;

namespace RechargeTools.Infrastructure
{
    public static class IDbContextExtensions
    {
        /// <summary>
        /// Detaches all entities from the current object context
        /// </summary>
        /// <param name="unchangedEntitiesOnly">When <c>true</c>, only entities in unchanged state get detached.</param>
        /// <returns>The count of detached entities</returns>
        public static int DetachAll(this ApplicationDbContext ctx, bool unchangedEntitiesOnly = true)
        {
            return ctx.DetachAll(unchangedEntitiesOnly);
        }

        public static void DetachEntities<TEntity>(this ApplicationDbContext ctx, IEnumerable<TEntity> entities) where TEntity : DbSet
        {
            Guard.NotNull(ctx, nameof(ctx));

            ctx.DetachEntities<DbSet>(entities);
        }

        public static IQueryable<TCollection> QueryForCollection<TEntity, TCollection>(
            this ApplicationDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, ICollection<TCollection>>> navigationProperty)
            where TEntity : DbSet
            where TCollection : DbSet
        {
            Guard.NotNull(entity, nameof(entity));
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            var dbContext = ctx as DbContext;
            if (dbContext == null)
            {
                throw new NotSupportedException("The ApplicationDbContext instance does not inherit from DbContext (EF)");
            }

            return dbContext.Entry(entity).Collection(navigationProperty).Query();
        }

        public static IQueryable<TProperty> QueryForReference<TEntity, TProperty>(
            this ApplicationDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty)
            where TEntity : DbSet
            where TProperty : DbSet
        {
            Guard.NotNull(entity, nameof(entity));
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            var dbContext = ctx as DbContext;
            if (dbContext == null)
            {
                throw new NotSupportedException("The ApplicationDbContext instance does not inherit from DbContext (EF)");
            }

            return dbContext.Entry(entity).Reference(navigationProperty).Query();
        }

        public static void LoadCollection<TEntity, TCollection>(
            this ApplicationDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, ICollection<TCollection>>> navigationProperty,
            bool force = false,
            Func<IQueryable<TCollection>, IQueryable<TCollection>> queryAction = null)
            where TEntity : GenericEntity
            where TCollection : GenericEntity
        {
            Guard.NotNull(entity, nameof(entity));
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            var dbContext = ctx as ApplicationDbContext;
            if (dbContext == null)
            {
                throw new NotSupportedException("The ApplicationDbContext instance does not inherit from DbContext (EF)");
            }

            var entry = dbContext.Entry(entity);
            var collection = entry.Collection(navigationProperty);

            // Avoid System.InvalidOperationException: Member 'IsLoaded' cannot be called for property...
            if (entry.State == EntityState.Detached)
            {
                List<TEntity> collections = new List<TEntity>();
                collections.Add(entity);
                ctx.AttachRange<TEntity>(collections);
            }

            if (force)
            {
                collection.IsLoaded = false;
            }

            if (!collection.IsLoaded)
            {
                if (queryAction != null)
                {
                    var query = collection.Query();

                    var myQuery = queryAction != null
                        ? queryAction(query)
                        : query;

                    collection.CurrentValue = myQuery.ToList();
                }
                else
                {
                    collection.Load();
                }

                collection.IsLoaded = true;
            }
        }

        public static void LoadReference<TEntity, TProperty>(
            this ApplicationDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty,
            bool force = false,
            Func<IQueryable<TProperty>, IQueryable<TProperty>> queryAction = null)
            where TEntity : GenericEntity
            where TProperty : GenericEntity
        {
            Guard.NotNull(entity, nameof(entity));
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            var dbContext = ctx as DbContext;
            if (dbContext == null)
            {
                throw new NotSupportedException("The ApplicationDbContext instance does not inherit from DbContext (EF)");
            }

            var entry = dbContext.Entry(entity);
            var reference = entry.Reference(navigationProperty);

            // Avoid System.InvalidOperationException: Member 'IsLoaded' cannot be called for property...
            if (entry.State == EntityState.Detached)
            {
                List<TEntity> collections = new List<TEntity>();
                collections.Add(entity);
                ctx.AttachRange<TEntity>(collections);
            }

            if (force)
            {
                reference.IsLoaded = false;
            }

            if (!reference.IsLoaded)
            {
                if (queryAction != null)
                {
                    var query = reference.Query();

                    var myQuery = queryAction != null
                        ? queryAction(query)
                        : query;

                    reference.CurrentValue = myQuery.FirstOrDefault();
                }
                else
                {
                    reference.Load();
                }

                reference.IsLoaded = true;
            }
        }

        public static void AttachRange<TEntity>(this ApplicationDbContext ctx, IEnumerable<TEntity> entities) where TEntity : GenericEntity
        {
            ctx.AttachRange(entities);
        }
    }
}