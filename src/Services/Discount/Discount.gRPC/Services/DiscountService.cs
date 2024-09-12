using AutoMapper;
using Discount.gRPC.Data;
using Discount.gRPC.Models;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Discount.gRPC.Services
{
    public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
    {
        private readonly DiscountContext _context;
        private readonly ILogger<DiscountService> _logger;
        private readonly IMapper _mapper;
        public DiscountService(DiscountContext context, ILogger<DiscountService> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }
        public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(e => e.ProductName == request.ProductName);

            if (coupon is null)
            {
                coupon = new Coupon { ProductName = request.ProductName, Amount = 0, Description = "Không giảm giá" };
            }
            _logger.LogInformation("Giảm giá được áp dụng cho sản phẩm : {productName}, Amount : {amount}", coupon.ProductName, coupon.Amount);

            var couponModel = _mapper.Map<CouponModel>(coupon);
            return couponModel;
        }

        public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
        {
            var coupon = _mapper.Map<Coupon>(request.Coupon);

            if (coupon is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Yêu cầu không hợp lệ !"));

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Thêm mới mã giảm giá thành công. Tên sản phẩm : {ProductName}", coupon.ProductName);

            var couponModel = _mapper.Map<CouponModel>(coupon);
            return couponModel;
        }
        public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
        {
            var coupon = _mapper.Map<Coupon>(request.Coupon);
            if (coupon is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Yêu cầu không hợp lệ !"));

            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cập nhật mã giảm giá thành công. Tên sản phẩm : {ProductName}", coupon.ProductName);

            var couponModel = _mapper.Map<CouponModel>(coupon);
            return couponModel;
        }

        public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(e => e.ProductName == request.ProductName);

            if (coupon is null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Không tim thấy mã giảm giá cho sản phẩm {request.ProductName}."));

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Xóa mã giảm giá thành công. Tên sản phẩm : {ProductName}", coupon.ProductName);

            return new DeleteDiscountResponse { Success = true };
        }
    }
}
