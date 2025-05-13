using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    public class LimitOrder : Order
    {
        public decimal Price { get; set; }  // Цена лимитного ордера
    }

}
