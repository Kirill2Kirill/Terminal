using MessagePack;
using Binance.Net.Interfaces;

namespace Terminal.Models
{
    /// <summary>
    /// Собственный класс нужен для того, чтобы были ключи к сохранению в MessagePack для скорости
    /// </summary>
    [MessagePackObject]
    public class HistoryKline : IBinanceKline
    {
        [Key(0)]
        public DateTime OpenTime { get; set; }

        [Key(1)]
        public decimal OpenPrice { get; set; }

        [Key(2)]
        public decimal HighPrice { get; set; }

        [Key(3)]
        public decimal LowPrice { get; set; }

        [Key(4)]
        public decimal ClosePrice { get; set; }

        [Key(5)]
        public decimal Volume { get; set; }

        [Key(6)]
        public DateTime CloseTime { get; set; }

        [Key(7)]
        public decimal QuoteVolume { get; set; }

        [Key(8)]
        public int TradeCount { get; set; }

        [Key(9)]
        public decimal TakerBuyBaseVolume { get; set; }

        [Key(10)]
        public decimal TakerBuyQuoteVolume { get; set; }
    }

}