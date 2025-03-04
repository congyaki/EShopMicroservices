using BuildingBlocks.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Extensions
{
    public static class Extension
    {
        public static async Task<IPagedList<T>> PagedList<T>(
            this IQueryable<T> values,
            int take,
            int skip,
            bool isAll = false,
            CancellationToken cancellationToken = default
        ) where T : class
        {
            if (isAll)
            {
                var allItems = await values.ToListAsync(cancellationToken);
                return new PagedList<T>
                {
                    Items = allItems,
                    Count = allItems.Count
                };
            }

            var pagedItems = await values.Skip(skip).Take(take).ToListAsync(cancellationToken);
            var totalCount = await values.CountAsync(cancellationToken);

            return new PagedList<T>
            {
                Items = pagedItems,
                Count = totalCount
            };
        }


    }
}
