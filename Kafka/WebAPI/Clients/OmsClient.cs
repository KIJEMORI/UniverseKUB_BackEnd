using Models.Dto.V1.Requests;
using System.Text;
using Models;
using Models.Dto.V1.Responses;
using static Common.JsonSerializeExtensions;

namespace WebAPI.Clients
{
    public class OmsClient(HttpClient client)
    {
        public async Task<V1AuditLogOrderResponse> LogOrder(V1AuditLogOrderRequest request, CancellationToken token)
        {
            var msg = await client.PostAsync("api/v1/order/log-order", new StringContent(request.ToJson(), Encoding.UTF8, "application/json"), token);
            if (msg.IsSuccessStatusCode)
            {
                var content = await msg.Content.ReadAsStringAsync(cancellationToken: token);
   
                return content.FromJson<V1AuditLogOrderResponse>();
            }

            throw new HttpRequestException();
        }
    }
}
