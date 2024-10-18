namespace Basket.API.Basket.GetBasket
{
    public record GetBasketQuery(string UserName) : IQuery<GetBasketResult>;
    public record GetBasketResult(ShoppingCart ShoppingCart);

    public class GetBasketQueryHandler : IQueryHandler<GetBasketQuery, GetBasketResult>
    {
        private readonly IBasketRepository _repository;

        public GetBasketQueryHandler(IBasketRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetBasketResult> Handle(GetBasketQuery query, CancellationToken cancellationToken)
        {
            var basket = await _repository.GetBasket(query.UserName, cancellationToken);

            return new GetBasketResult(basket);
        }
    }
}
