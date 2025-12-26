using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Dto.V1.Requests
{
    public class V1UpdateOrdersStatusRequest
    {

        public long[] OrderIds { get; set; }

        public string NewStatus { get; set; }
    }
}
