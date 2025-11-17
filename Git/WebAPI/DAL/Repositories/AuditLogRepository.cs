using Dapper;
using System.Text;
using WebAPI.DAL.Interfaces;
using WebAPI.DAL.Models;

namespace WebAPI.DAL.Repositories
{
    public class AuditLogRepository(UnitOfWork unitOfWork) : IAuditLogRepository
    {
        public async Task<V1AuditLogDal[]> BulkInsert(V1AuditLogDal[] model, CancellationToken token)
        {
            var sql = @"
            insert into audit_log_order
            (
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            )
            select 
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            from unnest(@Orders)
            returning 
                id,
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            ";

            var conn = await unitOfWork.GetConnection(token);
       
            var res = await conn.QueryAsync<V1AuditLogDal>(new CommandDefinition(
                sql, new { Orders = model }, cancellationToken: token));

            return res.ToArray();
        }

        public async Task<V1AuditLogDal[]> Query(QueryAuditLogDalModel model, CancellationToken token)
        {
            var sql = new StringBuilder(@"
                select 
                    id,
                    order_id,
                    order_item_id,
                    customer_id,
                    order_status,
                    created_at,
                    updated_at
                from audit_log_order
            ");

            // тот же динамический тип данных 
            var param = new DynamicParameters();

            // собираем условия для where
            var conditions = new List<string>();

            if (model.OrderId > 0)
            {
                param.Add("OrderIds", model.OrderId);
                conditions.Add("order_id = @OrderIds");
            }

            if (model.OrderItemId > 0)
            {
                param.Add("OrderItemIds", model.OrderItemId);
                conditions.Add("order_item_id = @OrderItemIds");
            }
            if (model.CustomerId > 0)
            {
                param.Add("CustomerIds", model.CustomerId);
                conditions.Add("customer_id = @CustomerIds");
            }
            if(model.OrderStatus != "")
            {
                param.Add("OrderStatus", model.OrderStatus);
                conditions.Add("order_status = @OrderStatus");
            }

            if (conditions.Count > 0)
            {
                // если условия есть, то добавляем в sql
                sql.Append(" where " + string.Join(" and ", conditions));
            }

            if (model.Limit > 0)
            {
                sql.Append(" limit @Limit");
                param.Add("Limit", model.Limit);
            }

            if (model.Offset > 0)
            {
                sql.Append(" offset @Offset");
                param.Add("Offset", model.Offset);
            }

            var conn = await unitOfWork.GetConnection(token);
            var res = await conn.QueryAsync<V1AuditLogDal>(new CommandDefinition(
                sql.ToString(), param, cancellationToken: token));

            return res.ToArray();
        }
    }
}
