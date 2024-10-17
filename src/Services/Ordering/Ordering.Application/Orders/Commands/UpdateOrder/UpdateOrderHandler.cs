namespace Ordering.Application.Orders.Commands.UpdateOrder
{
    public class UpdateOrderHandler : ICommandHandler<UpdateOrderCommand, UpdateOrderResult>
    {
        private readonly IApplicationDbContext _context;

        public UpdateOrderHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UpdateOrderResult> Handle(UpdateOrderCommand command, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Of(command.Order.Id);
            var order = await _context.Orders.FindAsync(orderId, cancellationToken);

            if (order is null)
            {
                throw new OrderNotFoundException(command.Order.Id);
            }

            UpdateOrderWithNewValues(order, command.Order);

            _context.Orders.Update(order);
            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateOrderResult(true);
        }

        private void UpdateOrderWithNewValues(Order order, OrderDto orderDto)
        {
            var updatedShippingAddress = Address.Of(
                orderDto.ShippingAddress.FirstName,
                orderDto.ShippingAddress.LastName,
                orderDto.ShippingAddress.EmailAddress,
                orderDto.ShippingAddress.AddressLine,
                orderDto.ShippingAddress.Country,
                orderDto.ShippingAddress.State,
                orderDto.ShippingAddress.ZipCode);

            var updatedBillingAddress = Address.Of(
                orderDto.BillingAddress.FirstName,
                orderDto.BillingAddress.LastName,
                orderDto.BillingAddress.EmailAddress,
                orderDto.BillingAddress.AddressLine,
                orderDto.BillingAddress.Country,
                orderDto.BillingAddress.State,
                orderDto.BillingAddress.ZipCode);

            var updatedPayment = Payment.Of(
                    orderDto.Payment.CardName,
                    orderDto.Payment.CardNumber,
                    orderDto.Payment.Expiration,
                    orderDto.Payment.Cvv,
                    orderDto.Payment.PaymantMethod);

            order.Update(
                orderName: OrderName.Of(orderDto.OrderName),
                shippingAddress: updatedShippingAddress,
                billingAddress: updatedBillingAddress,
                payment: updatedPayment,
                status: orderDto.Status
                );
        }
    }
}
