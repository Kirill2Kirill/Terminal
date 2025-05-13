using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    //{
    //  "symbol": "BTCUSDT",
    //  "exchanges": ["binance", "bybit", "okx"],
    //  "entryMarketOrder": { "action": "buy", "amount": 500 },
    //  "entryLimitOrders": [
    //    { "action": "buy", "amount": 200, "price": 45000 },
    //    { "action": "buy", "amount": 300, "price": 44000 }
    //  ],
    //  "exitLimitOrders": [
    //    { "action": "sell", "amount": 500, "price": 48000 }
    //  ],
    //  "exitMarketOrder": { "action": "sell", "amount": 500 }
    //}

    public class TradingSignal
    {
        public string Symbol { get; set; }
        public string[] Exchanges { get; set; }
        public string ClientId { get; set; } = $"trade_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

        public MarketOrder? EntryMarketOrder { get; set; }  // Вход по рынку
        public List<LimitOrder> EntryLimitOrders { get; set; } = new();  // Лимитные ордера на вход
        public List<LimitOrder> ExitLimitOrders { get; set; } = new();  // Лимитные ордера на выход
        public MarketOrder? ExitMarketOrder { get; set; }  // Выход по рынку

    }
}
