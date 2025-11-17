namespace WebAPI.DAL.Models
{
    public class QueryAuditLogDalModel
    {
        public long OrderId { get; set; }
        public long OrderItemId { get; set; }
        public long CustomerId { get; set; }
        public string OrderStatus { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }
}
