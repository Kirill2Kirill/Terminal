using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net.Objects.Sockets;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Interfaces.Clients.SpotApi;
using Binance.Net.Interfaces.Clients.UsdFuturesApi;

namespace Terminal.Managers
{
    public class BinanceTradingProcessor
    {
        private readonly BinanceSocketClient _binanceSocketClient;

        public IBinanceSocketClientSpotApiTrading Spot => _binanceSocketClient.SpotApi.Trading;
        public IBinanceSocketClientUsdFuturesApiTrading Futures => _binanceSocketClient.UsdFuturesApi.Trading;

        public BinanceTradingProcessor(BinanceSocketClient binanceSocketClient)
        {
            _binanceSocketClient = binanceSocketClient;
        }
    }
}
