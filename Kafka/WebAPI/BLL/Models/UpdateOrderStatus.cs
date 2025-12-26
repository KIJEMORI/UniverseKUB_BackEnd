namespace WebAPI.DAL.Models
{
    public class UpdateOrderStatus
    {
        public long[] OrderIds { get; set; }
        public string Status { get; set; }
    }
}
