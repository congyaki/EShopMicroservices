using AutoMapper;
using BuildingBlocks.Messaging.Events;

namespace Basket.API.Automapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ShoppingCart, BasketCheckoutEvent>();
        }
    }
}
