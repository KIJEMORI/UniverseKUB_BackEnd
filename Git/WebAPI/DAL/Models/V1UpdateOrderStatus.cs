namespace WebAPI.DAL.Models
{
    public class V1UpdateOrderStatus
    {
        public long[] OrderIds { get; set; }
        public string Status { get; set; }
    }
}
