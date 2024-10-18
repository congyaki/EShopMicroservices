namespace Basket.API.Basket.DeleteBasket
{
    public record DeleteBasketCommand(string UserName) : ICommand<DeleteBasketResult>;
    public record DeleteBasketResult(bool IsSuccess);
    public class DeleteBasketCommandValidator : AbstractValidator<DeleteBasketCommand>
    {
        public DeleteBasketCommandValidator()
        {
            RuleFor(e => e.UserName)
                .NotEmpty().WithMessage("Không được để trống tên người dùng !");
        }
    }
    public class DeleteBasketCommandHandler : ICommandHandler<DeleteBasketCommand, DeleteBasketResult>
    {
        private readonly IBasketRepository _repository;

        public DeleteBasketCommandHandler(IBasketRepository repository)
        {
            _repository = repository;
        }

        public async Task<DeleteBasketResult> Handle(DeleteBasketCommand command, CancellationToken cancellationToken)
        {
            await _repository.DeleteBasket(command.UserName, cancellationToken);

            return new DeleteBasketResult(true);
        }
    }
}
