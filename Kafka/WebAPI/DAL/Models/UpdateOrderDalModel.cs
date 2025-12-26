namespace WebAPI.DAL.Models
{
    public class UpdateOrderDalModel
    {
        public long[] Ids { get; set; }

        public string NewStatus { get; set; }
    }
}
