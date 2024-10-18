using BuildingBlocks.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Application.Orders.Commands.DeleteOrder;
using Ordering.Application.Orders.Commands.UpdateOrder;
using Ordering.Application.Orders.Queries.GetOrders;
using Ordering.Application.Orders.Queries.GetOrdersByCustomer;
using Ordering.Application.Orders.Queries.GetOrdersByName;

namespace Ordering.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private ISender _mediator = null!;
        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
        public OrdersController(ISender mediator)
        {
            _mediator = mediator;
        }

        #region Query
        [ProducesResponseType(typeof(GetOrdersResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet]
        public async Task<IActionResult> GetOrders(PaginationRequest request)
        {
            var data = await _mediator.Send(new GetOrdersQuery(request));

            return Ok(data);
        }

        [ProducesResponseType(typeof(GetOrdersByCustomerResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("/customer/{customerId}")]
        public async Task<IActionResult> GetOrdersByCustomerId(Guid customerId)
        {
            var data = await _mediator.Send(new GetOrdersByCustomerIdQuery(customerId));

            return Ok(data);
        }

        [ProducesResponseType(typeof(GetOrdersByNameQuery), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{orderName}")]
        public async Task<IActionResult> GetOrdersByName(string orderName)
        {
            var data = await _mediator.Send(new GetOrdersByNameQuery(orderName));

            return Ok(data);
        }
        #endregion

        #region Command
        [ProducesResponseType(typeof(CreateOrderResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderCommand request)
        {
            var data = await _mediator.Send(request);

            return Ok(data);
        }

        [ProducesResponseType(typeof(UpdateOrderResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut]
        public async Task<IActionResult> UpdateOrder(UpdateOrderCommand request)
        {
            var data = await _mediator.Send(request);

            return Ok(data);
        }

        [ProducesResponseType(typeof(DeleteOrderResult), StatusCodes.Status200OK)]
        [ProducesDefaultResponseType()]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(DeleteOrderCommand request)
        {
            var data = await _mediator.Send(request);
            return Ok(data);
        }
        #endregion
    }
}
