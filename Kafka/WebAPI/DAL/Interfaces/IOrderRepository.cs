using WebAPI.DAL.Models;

namespace WebAPI.DAL.Interfaces
{
    public interface IOrderRepository
    {
        Task<V1OrderDal[]> BulkInsert(V1OrderDal[] model, CancellationToken token);

        Task<V1OrderDal[]> Query(QueryOrdersDalModel model, CancellationToken token);

        Task<V1OrderDal[]> Update(UpdateOrderDalModel model, CancellationToken token);
    }
}
