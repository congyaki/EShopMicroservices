namespace Ordering.Application.Orders.Commands.CreateOrder
{
    public record CreateOrderCommand(OrderDto Order) : ICommand<CreateOrderResult>;

    public record CreateOrderResult(Guid Id);

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(o => o.Order.OrderName)
                .NotEmpty().WithMessage("Không được để trống tên");
            RuleFor(o => o.Order.CustomerId)
                .NotNull().WithMessage("Không được để trống khách hàng");
            RuleFor(o => o.Order.OrderItems)
                .NotEmpty().WithMessage("Không được để trống chi tiết đơn hàng");
        }
    }
}
