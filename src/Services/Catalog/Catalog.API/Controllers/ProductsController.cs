using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.DeleteProducts;
using Catalog.API.Products.GetProductById;
using Catalog.API.Products.GetProducts;
using Catalog.API.Products.GetProductsByCategory;
using Catalog.API.Products.UpdateProduct;
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

        #region R
        [HttpGet]
        [Route("get-list")]
        public async Task<IActionResult> GetList([FromQuery] int? pageNumber = 1, int? pageSize = 10)
        {
            var data = await _mediator.Send(new GetProductsQuery()
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            return Ok(data);
        }

        [HttpGet]
        [Route("get-by-id")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await _mediator.Send(new GetProductByIdQuery(id));

            return Ok(data);
        }

        [HttpGet]
        [Route("get-by-category")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var data = await _mediator.Send(new GetProductsByCategoryQuery(category));

            return Ok(data);
        }
        #endregion

        #region CUD
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(CreateProductCommand command)
        {
            var data = await _mediator.Send(command);

            return Ok(data);
        }

        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> Update(UpdateProductCommand command)
        {
            var data = await _mediator.Send(command);

            return Ok(data);
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> Delete(IEnumerable<Guid> ids)
        {
            var data = await _mediator.Send(new DeleteProductsCommand(ids));

            return Ok(data);
        }
        #endregion
    }
}
