using Basket.API.Data;
using Basket.API.Models;
using BuildingBlocks.CQRS;

namespace Basket.API.Basket.StoreBasket
{
    public record StoreBasketCommand(ShoppingCart Cart) : ICommand<StoreBasketResult>;
    public record StoreBasketResult(string UserName);
    public class StoreBasketCommandValidator : AbstractValidator<StoreBasketCommand>
    {
        public StoreBasketCommandValidator()
        {
            RuleFor(e => e.Cart)
                .NotNull().WithMessage("Không được để trống giỏ hàng !");
            RuleFor(e => e.Cart.UserName)
                .NotEmpty().WithMessage("Không được để trống tên người dùng !");
        }
    }
    public class StoreBasketCommandHandler : ICommandHandler<StoreBasketCommand, StoreBasketResult>
    {
        private readonly IBasketRepository _repository;

        public StoreBasketCommandHandler(IBasketRepository repository)
        {
            _repository = repository;
        }

        public async Task<StoreBasketResult> Handle(StoreBasketCommand command, CancellationToken cancellationToken)
        {
            await _repository.StoreBasket(command.Cart, cancellationToken);

            return new StoreBasketResult(command.Cart.UserName);
        }
    }
}
