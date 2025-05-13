using Binance.Net;
using Bybit.Net;
using OKX.Net;
using CryptoExchange.Net.Authentication;
using System;
using Binance.Net.Clients;
using Bybit.Net.Clients;
using OKX.Net.Clients;
using Binance.Net.Objects.Options;
using Bybit.Net.Objects.Options;
using OKX.Net.Objects.Options;

namespace WebHooksBridge
{
    public static class OrderManager
    {
        private static readonly ExchangeClientsManager clientsManager = new ExchangeClientsManager();

        public static async Task ProcessOrdersAsync(string exchange, TradingSignal signal)
        {
            try
            {
                if (signal.EntryMarketOrder != null)
                    await PlaceMarketOrderAsync(exchange, signal.Symbol, signal.EntryMarketOrder);

                //if (signal.EntryLimitOrder != null)
                //    await PlaceLimitOrderAsync(exchange, signal.Symbol, signal.EntryLimitOrder);

                //if (!string.IsNullOrEmpty(signal.CancelOrderClientId))
                //    await CancelOrderAsync(exchange, signal.Symbol, signal.CancelOrderClientId);

                await Logger.LogSuccessAsync($"✅ Все ордеры обработаны для {exchange}");
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync($"❌ Ошибка обработки ордеров для {exchange}", ex);
            }
        }


        public static async Task PlaceMarketOrderAsync(string exchange, string symbol, MarketOrder order)
        {
            try
            {
                var client = await clientsManager.GetClientAsync<object>(exchange);
                if (client == null)
                {
                    await Logger.LogErrorAsync($"Не удалось получить клиента биржи '{exchange}'.");
                    return;
                }

                await Logger.LogSuccessAsync($"Выставляем рыночный ордер: {exchange}, {symbol}, {order.Action}, {order.Amount}, ClientId: {order.ClientId}");

                if (client is BinanceRestClient binanceClient)
                {
                    //await binanceClient.SpotApi.Trading.PlaceOrderAsync(
                    //    symbol,
                    //    order.Action == OrderAction.Buy ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell,
                    //    Binance.Net.Enums.OrderType.Market,
                    //    order.Amount,
                    //    clientOrderId: order.ClientId
                    //);
                }
                else if (client is BybitRestClient bybitClient)
                {
                    //await bybitClient.SpotApi.Trading.PlaceOrderAsync(
                    //    symbol,
                    //    order.Action == OrderAction.Buy ? Bybit.Net.Enums.OrderSide.Buy : Bybit.Net.Enums.OrderSide.Sell,
                    //    Bybit.Net.Enums.OrderType.Market,
                    //    order.Amount,
                    //    clientOrderId: order.ClientId
                    //);
                }
                else if (client is OKXRestClient okxClient)
                {
                    //await okxClient.TradeApi.PlaceOrderAsync(
                    //    symbol,
                    //    order.Action == OrderAction.Buy ? OKX.Net.Enums.OrderSide.Buy : OKX.Net.Enums.OrderSide.Sell,
                    //    OKX.Net.Enums.OrderType.Market,
                    //    order.Amount,
                    //    clientOrderId: order.ClientId
                    //);
                }

                await Logger.LogSuccessAsync($"Рыночный ордер успешно отправлен на {exchange}");
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync($"Ошибка при выставлении рыночного ордера на '{exchange}'", ex);
            }
        }

        public static async Task PlaceLimitOrderAsync(string exchange, string symbol, LimitOrder order)
        {
            try
            {
                var client = await clientsManager.GetClientAsync<object>(exchange);
                if (client == null)
                {
                    await Logger.LogErrorAsync($"Не удалось получить клиента биржи '{exchange}'.");
                    return;
                }

                await Logger.LogSuccessAsync($"Выставляем лимитный ордер: {exchange}, {symbol}, {order.Action}, {order.Amount}, Цена: {order.Price}, ClientId: {order.ClientId}");

                if (client is BinanceRestClient binanceClient)
                {
                    //await binanceClient.SpotApi.Trading.PlaceOrderAsync(
                    //    symbol,
                    //    order.Action == OrderAction.Buy ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell,
                    //    Binance.Net.Enums.OrderType.Limit,
                    //    order.Amount,
                    //    price: order.Price,
                    //    clientOrderId: order.ClientId
                    //);
                }
                else if (client is BybitRestClient bybitClient)
                {
                    //await bybitClient.SpotApi.Trading.PlaceOrderAsync(
                    //    symbol,
                    //    order.Action == OrderAction.Buy ? Bybit.Net.Enums.OrderSide.Buy : Bybit.Net.Enums.OrderSide.Sell,
                    //    Bybit.Net.Enums.OrderType.Limit,
                    //    order.Amount,
                    //    price: order.Price,
                    //    clientOrderId: order.ClientId
                    //);
                }
                else if (client is OKXRestClient okxClient)
                {
                    //await okxClient.TradeApi.PlaceOrderAsync(
                    //    symbol,
                    //    order.Action == OrderAction.Buy ? OKX.Net.Enums.OrderSide.Buy : OKX.Net.Enums.OrderSide.Sell,
                    //    OKX.Net.Enums.OrderType.Limit,
                    //    order.Amount,
                    //    price: order.Price,
                    //    clientOrderId: order.ClientId
                    //);
                }

                await Logger.LogSuccessAsync($"Лимитный ордер успешно отправлен на {exchange}");
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync($"Ошибка при выставлении лимитного ордера на '{exchange}'", ex);
            }
        }

        public static async Task CancelOrderAsync(string exchange, string symbol, string clientId)
        {
            try
            {
                var client = await clientsManager.GetClientAsync<object>(exchange);
                if (client == null)
                {
                    await Logger.LogErrorAsync($"Не удалось получить клиента биржи '{exchange}'.");
                    return;
                }

                await Logger.LogSuccessAsync($"Отменяем ордер: {exchange}, {symbol}, ClientId: {clientId}");

                if (client is BinanceRestClient binanceClient)
                {
                    //await binanceClient.SpotApi.Trading.CancelOrderAsync(symbol, origClientOrderId: clientId);
                }
                else if (client is BybitRestClient bybitClient)
                {
                    //await bybitClient.SpotApi.Trading.CancelOrderAsync(symbol, clientOrderId: clientId);
                }
                else if (client is OKXRestClient okxClient)
                {
                    //await okxClient.TradeApi.CancelOrderAsync(symbol, clientOrderId: clientId);
                }

                await Logger.LogSuccessAsync($"Ордер успешно отменен на {exchange}");
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync($"Ошибка при отмене ордера на '{exchange}'", ex);
            }
        }
    }
}
