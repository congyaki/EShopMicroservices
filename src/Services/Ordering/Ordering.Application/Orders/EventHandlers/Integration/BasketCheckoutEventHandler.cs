using BuildingBlocks.Messaging.Events;
using MassTransit;
using Ordering.Application.Orders.Commands.CreateOrder;

namespace Ordering.Application.Orders.EventHandlers.Integration
{
    public class BasketCheckoutEventHandler : IConsumer<BasketCheckoutEvent>
    {
        private readonly ISender _mediator;
        private readonly ILogger<BasketCheckoutEventHandler> _logger;
        public BasketCheckoutEventHandler(ISender mediator, ILogger<BasketCheckoutEventHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
        {
            _logger.LogInformation("Integration Event handled: {IntegrationEvent}", context.Message.GetType().Name);

            var command = MaptoCreateOrderCommand(context.Message);
            await _mediator.Send(command);
        }

        private CreateOrderCommand MaptoCreateOrderCommand(BasketCheckoutEvent message)
        {
            var shippingAddressDto = new AddressDto(message.FirstName, message.LastName, message.EmailAddress, message.AddressLine, message.Country, message.State, message.ZipCode);

            var billingAddressDto = new AddressDto(message.FirstName, message.LastName, message.EmailAddress, message.AddressLine, message.Country, message.State, message.ZipCode);

            var paymentDto = new PaymentDto(message.CardName, message.CardNumber, message.Expiration, message.CVV, message.PaymentMethod);

            var orderId = Guid.NewGuid();

            List<OrderItemDto> orderItems = new List<OrderItemDto>()
            {
                new OrderItemDto(orderId, new Guid("5334c996-8457-4cf0-815c-ed2b77c4ff61"), 2, 500),
                new OrderItemDto(orderId, new Guid("c67d6323-e8b1-4bdf-9a75-b0d0d2e7e914"), 1, 400)
            };
                              ;
            var orderDto = new OrderDto(
                Id: orderId,
                CustomerId: message.CustomerId,
                OrderName: message.UserName,
                ShippingAddress: shippingAddressDto,
                BillingAddress: billingAddressDto,
                Payment: paymentDto,
                Status: Ordering.Domain.Enums.OrderStatus.Pending,
                OrderItems: orderItems);

            return new CreateOrderCommand(orderDto);
        }
    }
}
