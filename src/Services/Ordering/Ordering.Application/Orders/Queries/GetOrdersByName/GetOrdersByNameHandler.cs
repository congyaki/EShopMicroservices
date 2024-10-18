
using Ordering.Application.Extensions;

namespace Ordering.Application.Orders.Queries.GetOrdersByName
{
    public class GetOrdersByNameHandler : IQueryHandler<GetOrdersByNameQuery, GetOrdersByNameResult>
    {
        private readonly IApplicationDbContext _context;

        public GetOrdersByNameHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetOrdersByNameResult> Handle(GetOrdersByNameQuery query, CancellationToken cancellationToken)
        {
            var orders = await _context.Orders
                            .Include(o => o.OrderItems)
                            .AsNoTracking()
                            .Where(o => o.OrderName.Value.Contains(query.Name))
                            .OrderBy(o => o.OrderName.Value)
                            .ToListAsync(cancellationToken);

            return new GetOrdersByNameResult(orders.ToOrderDtoList());
        }
    }
}
