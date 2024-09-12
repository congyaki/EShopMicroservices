using AutoMapper;
using Discount.gRPC.Models;

namespace Discount.gRPC.Automapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CouponModel, Coupon>();
            CreateMap<Coupon, CouponModel>();
        }
    }
}
