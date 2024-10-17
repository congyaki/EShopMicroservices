namespace Ordering.Application.Orders.Commands.UpdateOrder
{
    public record UpdateOrderCommand(OrderDto Order) : ICommand<UpdateOrderResult>;

    public record UpdateOrderResult(bool IsSuccess);

    public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
    {
        public UpdateOrderCommandValidator()
        {
            RuleFor(o => o.Order.Id)
                .NotEmpty().WithMessage("Không được để trống Id");
            RuleFor(o => o.Order.OrderName)
                .NotEmpty().WithMessage("Không được để trống tên");
            RuleFor(o => o.Order.CustomerId)
                .NotNull().WithMessage("Không được để trống khách hàng");
            
        }
    }
}
