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
        public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery request)
        {
            var data = await _mediator.Send(request);

            return Ok(data);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var data = await _mediator.Send(new GetProductByIdQuery(id));

            return Ok(data);
        }

        [HttpGet]
        [Route("category/{category}")]
        public async Task<IActionResult> GetProductByCategory(string category)
        {
            var data = await _mediator.Send(new GetProductsByCategoryQuery(category));

            return Ok(data);
        }
        #endregion

        #region CUD
        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductCommand command)
        {
            var data = await _mediator.Send(command);

            return Ok(data);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProduct(UpdateProductCommand command)
        {
            var data = await _mediator.Send(command);

            return Ok(data);
        }

        [HttpDelete]
        [Route("")]
        public async Task<IActionResult> Delete([FromBody] IEnumerable<Guid> ids)
        {
            var data = await _mediator.Send(new DeleteProductsCommand(ids));

            return Ok(data);
        }
        #endregion
    }
}
