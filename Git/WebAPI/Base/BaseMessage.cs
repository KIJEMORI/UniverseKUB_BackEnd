namespace WebAPI.Consumer.Base
{
    public abstract class BaseMessage
    {
        public abstract string RoutingKey { get; }
    }
}
