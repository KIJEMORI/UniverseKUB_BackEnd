
using WebAPI.BLL.Models;
using WebAPI.Consumer.Base;

namespace WebAPI.Base
{
    public class OmsOrderCreatedMessage : BaseMessage
    {
        public OrderUnit Message { get; set; }


    }
}
