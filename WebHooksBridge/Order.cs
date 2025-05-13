using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    public abstract class Order
    {
        public OrderAction Action { get; set; }  // "buy" или "sell"
        public decimal Amount { get; set; }
        public string ClientId { get; set; } = Guid.NewGuid().ToString(); // Уникальный ID ордера
    }

}
