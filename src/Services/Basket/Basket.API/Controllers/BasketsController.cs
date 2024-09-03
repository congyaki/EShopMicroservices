using Basket.API.Basket.DeleteBasket;
using Basket.API.Basket.GetBasket;
using Basket.API.Basket.StoreBasket;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketsController : ControllerBase
    {
        private ISender _mediator = null!;
        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
        public BasketsController(ISender mediator)
        {
            _mediator = mediator;
        }

        #region R
        
        [HttpGet]
        [Route("{userName}")]
        public async Task<IActionResult> GetBasketByUserName(string userName)
        {
            var data = await _mediator.Send(new GetBasketQuery(userName));

            return Ok(data);
        }

        #endregion

        #region CUD

        [HttpPost]
        public async Task<IActionResult> StoreBasket(StoreBasketCommand command)
        {
            var data = await _mediator.Send(command);

            return Ok(data);
        }

        [HttpDelete]
        [Route("{userName}")]
        public async Task<IActionResult> DeleteBasket(string userName)
        {
            var data = await _mediator.Send(new DeleteBasketCommand(userName));

            return Ok(data);
        }
        #endregion
    }
}
