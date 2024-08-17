

using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.GetProducts;

namespace Catalog.API.Automapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateProductCommand, Product>();
            CreateMap<Product, GetProductsResult>();
        }
    }
}
