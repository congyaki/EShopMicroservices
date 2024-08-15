using Catalog.API.Products.CreateProduct;
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

        public async Task<Guid> Create(CreateProductCommand command)
        {
           var data = await _mediator.Send(command);

            return data;
        }

    }
}
