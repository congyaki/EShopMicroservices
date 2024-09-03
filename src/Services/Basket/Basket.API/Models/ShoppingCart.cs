namespace Basket.API.Models
{
    public class ShoppingCart
    {
        public string UserName { get; set; } = default!;
        public List<ShoppingCartItem> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(e => e.Price * e.Quantity);

        public ShoppingCart()
        {
            
        }

        public ShoppingCart(string userName)
        {
            UserName = userName;
        }
    }
}
