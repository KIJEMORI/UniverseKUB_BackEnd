using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;
using WebAPI.BLL.Models;
using WebAPI.BLL.Services;
using WebAPI.Validators;

namespace WebAPI.Controllers.V1
{

    [Route("api/v1/order")]
    public class OrderController(OrderService orderService, ValidatorFactory validatorFactory) : ControllerBase
    {
        [HttpPost("batch-create")]
        public async Task<ActionResult<V1CreateOrderResponse>> V1BatchCreate([FromBody] V1CreateOrderRequest request, CancellationToken token)
        {
            /*var validationResult = await validatorFactory.GetValidator<V1CreateOrderRequest>().ValidateAsync(request, token);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary());
            }*/

            var res = await orderService.BatchInsert(request.Orders.Select(x => new OrderUnit
            {
                CustomerId = x.CustomerId,
                DeliveryAddress = x.DeliveryAddress,
                TotalPriceCents = x.TotalPriceCents,
                TotalPriceCurrency = x.TotalPriceCurrency,
                OrderItems = x.OrderItems.Select(p => new OrderItemUnit
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    ProductTitle = p.ProductTitle,
                    ProductUrl = p.ProductUrl,
                    PriceCents = p.PriceCents,
                    PriceCurrency = p.PriceCurrency,
                }).ToArray()
            }).ToArray(), token);


            return Ok(new V1CreateOrderResponse
            {
                Orders = Map(res)
            });
        }

        [HttpPost("batch-update")]
        public async Task<ActionResult<V1UpdateOrderStatusResponse>> V1BatchUpadate([FromBody] V1UpdateOrdersStatusRequest request, CancellationToken token)
        {
            /*var validationResult = await validatorFactory.GetValidator<V1CreateOrderRequest>().ValidateAsync(request, token);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary());
            }*/
            try
            {
                await orderService.UpdateOrderStatus(
                    [new DAL.Models.V1UpdateOrderStatus
                    {
                        Status = request.NewStatus,
                        OrderIds = request.OrderIds
                    }],
                    token
                );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


            return Ok(new V1UpdateOrderStatusResponse
            {

            });
        }


        [HttpGet("query")]
        public async Task<ActionResult<V1QueryOrdersResponse>> V1QueryOrders([FromBody] V1QueryOrdersRequest request, CancellationToken token)
        {
            var validationResult = await validatorFactory.GetValidator<V1QueryOrdersRequest>().ValidateAsync(request, token);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary());
            }
            var res = await orderService.GetOrders(new QueryOrderItemsModel
            {
                Ids = request.Ids,
                CustomerIds = request.CustomerIds,
                Page = request.Page,
                PageSize = request.PageSize,
                IncludeOrderItems = request.IncludeOrderItems
            }, token);

            return Ok(new V1QueryOrdersResponse
            {
                Orders = Map(res)
            });
        }

        private Models.Dto.Common.OrderUnit[] Map(OrderUnit[] orders)
        {
            return orders.Select(x => new Models.Dto.Common.OrderUnit
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                DeliveryAddress = x.DeliveryAddress,
                TotalPriceCents = x.TotalPriceCents,
                TotalPriceCurrency = x.TotalPriceCurrency,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                OrderItems = x.OrderItems.Select(p => new Models.Dto.Common.OrderItemUnit
                {
                    Id = p.Id,
                    OrderId = p.OrderId,
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    ProductTitle = p.ProductTitle,
                    ProductUrl = p.ProductUrl,
                    PriceCents = p.PriceCents,
                    PriceCurrency = p.PriceCurrency,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToArray()
            }).ToArray();
        }

       [HttpPost("log-order")]
        public async Task<ActionResult<V1AuditLogOrderResponse>> V1GetOrderByAudit([FromBody] V1AuditLogOrderRequest request, CancellationToken token)
        {
            /*var validationResult = await validatorFactory.GetValidator<V1AuditLogOrderRequest>().ValidateAsync(request, token);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary());
            }*/
            var res = await orderService.LogOrder(request.Orders.Select(x => new AuditLogUnit
            {
                OrderId = x.OrderId,
                OrderItemId = x.OrderItemId,
                CustomerId = x.CustomerId,
                OrderStatus = x.OrderStatus
            }).ToArray(), token);

            return Ok(new V1QueryOrdersResponse
            {
                Orders = Map(res)
            });

            /*
            return Ok(new V1AuditLogResponse
            {
                Orders = Map(res.Select(x=>new OrderUnit
                {
                    CustomerId = x.CustomerId,
                    Id = x.OrderId,
                }).ToArray())
            });*/
        }
    }
}
