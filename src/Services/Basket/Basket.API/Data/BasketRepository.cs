namespace Basket.API.Data
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDocumentSession _session;
        public BasketRepository(IDocumentSession session)
        {
            _session = session;
        }
        public async Task<ShoppingCart> GetBasket(string userName, CancellationToken cancellationToken = default)
        {
            var basket = await _session.LoadAsync<ShoppingCart>(userName, cancellationToken);

            return basket is null ? throw new BasketNotFoundException(userName) : basket;
        }

        public async Task<ShoppingCart> StoreBasket(ShoppingCart cart, CancellationToken cancellationToken = default)
        {
            _session.Store(cart);
            await _session.SaveChangesAsync(cancellationToken);

            return cart;
        }
        public async Task<bool> DeleteBasket(string userName, CancellationToken cancellationToken = default)
        {
            _session.Delete<ShoppingCart>(userName);
            await _session.SaveChangesAsync(cancellationToken);

            return true;
        }

    }
}
