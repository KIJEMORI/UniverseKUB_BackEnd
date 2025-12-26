using FluentMigrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migrations
{
    [Migration (3)]
    public class UpdateOrderTable : Migration
    {
        public override void Up()
        {
  
            Execute.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS status TEXT NOT NULL DEFAULT 'created';");
            Execute.Sql(@"
                DROP TYPE IF EXISTS v1_order CASCADE; 
                CREATE TYPE v1_order AS (
                    id bigint,
                    customer_id bigint,
                    delivery_address text,
                    total_price_cents bigint,
                    total_price_currency text,
                    status text,
                    created_at timestamp with time zone,
                    updated_at timestamp with time zone
                );
            ");
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }

    }
}
