namespace Ordering.Domain.Models
{
    public class Product : Entity<ProductId>
    {
        public string Name { get; private set; } = default!;
        public decimal Price { get; private set; } = default!;

        public static Product Create(ProductId id, string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (price <= 0)
            {
                throw new ArgumentNullException(nameof(price));
            }

            var product = new Product()
            {
                Id = id,
                Name = name,
                Price = price,
            };

            return product;
        }
    }
}
