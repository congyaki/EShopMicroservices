using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.GetProducts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private ISender _mediator = null!;

        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
        public ProductsController(ISender mediator)
        {
            _mediator = mediator;
        }

        #region CUD
        [HttpPost]
        public async Task<Guid> Create(CreateProductCommand command)
        {
            var data = await _mediator.Send(command);

            return data;
        }
        #endregion

        #region R
        [HttpGet]
        public async Task<GetProductsResult> GetList([FromQuery] int? pageNumber = 1, int? pageSize = 10)
        {
            var data = await _mediator.Send(new GetProductsQuery()
            {
                pageNumber = pageNumber,
                pageSize = pageSize
            });

            return data;
        }
        #endregion

    }
}
