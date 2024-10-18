
using Ordering.Application.Extensions;

namespace Ordering.Application.Orders.Queries.GetOrdersByCustomer
{
    public class GetOrdersByCustomerIdHandler : IQueryHandler<GetOrdersByCustomerIdQuery, GetOrdersByCustomerResult>
    {
        private readonly IApplicationDbContext _context;

        public GetOrdersByCustomerIdHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetOrdersByCustomerResult> Handle(GetOrdersByCustomerIdQuery query, CancellationToken cancellationToken)
        {
            var orders = await _context.Orders
                            .Include(o => o.OrderItems)
                            .AsNoTracking()
                            .Where(o => o.CustomerId == CustomerId.Of(query.CustomerId))
                            .OrderBy(o => o.OrderName.Value)
                            .ToListAsync(cancellationToken);

            return new GetOrdersByCustomerResult(orders.ToOrderDtoList());
        }
    }
}
