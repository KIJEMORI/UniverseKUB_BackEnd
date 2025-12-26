using WebAPI.Consumer.Base;
using WebAPI.DAL.Models;

namespace WebAPI.Base
{
    public class OmsOrderStatusChangedMessage : BaseMessage
    {
        public UpdateOrderStatus Message { get; set; }
    }
}
