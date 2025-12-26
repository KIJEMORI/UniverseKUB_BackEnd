using System.Text.Json.Serialization;

namespace WebAPI.Consumer.Config
{

    public class RabbitMqEnvelope<T>
    {

        public T Message { get; set; }

        public string RoutingKey { get; set; }
    }
}
