using WebAPI.DAL.Models;

namespace WebAPI.DAL.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<V1AuditLogDal[]> BulkInsert(V1AuditLogDal[] model, CancellationToken token);

        Task<V1AuditLogDal[]> Query(QueryAuditLogDalModel model, CancellationToken token);

        Task Update(V1UpdateOrderStatus model, CancellationToken token);
    }
}
