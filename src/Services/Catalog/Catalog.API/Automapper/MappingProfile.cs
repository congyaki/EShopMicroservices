using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.GetProducts;
using Catalog.API.Products.UpdateProduct;

namespace Catalog.API.Automapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateProductCommand, Product>();
            CreateMap<UpdateProductCommand, Product>();
        }
    }
}
