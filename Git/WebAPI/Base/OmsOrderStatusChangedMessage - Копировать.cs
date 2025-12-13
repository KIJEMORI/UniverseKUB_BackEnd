using WebAPI.Consumer.Base;
using WebAPI.DAL.Models;

namespace WebAPI.Base
{
    public class OmsOrderStatusChangedMessage : BaseMessage
    {
        override public string RoutingKey { get; } = "order.status.changed";

        public UpdateOrderStatus Message { get; set; }
    }
}
