
using WebAPI.BLL.Models;
using WebAPI.Consumer.Base;

namespace WebAPI.Base
{
    public class OmsOrderCreatedMessage : BaseMessage
    {
        override public string RoutingKey { get; } = "order.created";
        public OrderUnit Message { get; set; }


    }
}
