using System.Runtime.Intrinsics.X86;
using WebhookReceiver;

namespace WebhookReceiver
{
    public class TradingViewSignal
    {
        public string User { get; set; }
        public List<SignalExchange>? Exchanges { get; set; }
        public List<SignalOrder>? Orders { get; set; }
        public MarketPosition? MarketPosition { get; set; }
        public string HookKey { get; set; }
        public DateTime SignalFireTime { get; set; }

    }

    public enum SignalExchange
    {
        Binance,
        Bybit,
        OKX,
        Kucoin,
        Bitfinex,
        Kraken,
        Huobi,
        Gateio,
        Bittrex,
        Poloniex,
        Bitstamp,
    }

    public enum SignalSide
    {
        Buy,
        Sell,
    }

    public enum MarketPosition
    {
        Long,
        Short,
        Flat,
    }

    public enum SignalOrderType
    {
        Market,
        Limit,
        StopMarket,
        StopLimit,
    }

    public abstract class SignalOrder
    {
        public SignalSide Side { get; set; }  // "buy" или "sell"
        
        public MarketPosition? MarketPosition {get;set;}

        public SignalOrderType? OrderType { get; set; }
        public decimal? UsdtQty { get; set; }
        public decimal? PercentFromPosition { get; set; }
        public decimal? Price { get; set; } // Цена, по которой нужно купить/продать
        public string ClientOrderId { get; set; } = ClientOrderIdGenerator.Generate();

    }

}

