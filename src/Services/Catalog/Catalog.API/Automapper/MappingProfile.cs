

using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;

namespace Catalog.API.Automapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateProductCommand, Product>();
        }
    }
}
