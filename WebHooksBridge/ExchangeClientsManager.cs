using Binance.Net.Clients;
using Bybit.Net.Clients;
using OKX.Net.Clients;
using CryptoExchange.Net.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OKX.Net;
using OKX.Net.Objects;

namespace WebHooksBridge
{   
    public class ExchangeClientsManager
    {
        private readonly Dictionary<string, object> clients = new();

        public async Task<T?> GetClientAsync<T>(string exchange) where T : class
        {
            exchange = exchange.ToLower();

            if (clients.TryGetValue(exchange, out var existingClient))
                return existingClient as T;

            var newClient = await CreateClientAsync<T>(exchange);
            if (newClient != null)
                clients[exchange] = newClient;

            return (T)newClient;
        }

        private async Task<object?> CreateClientAsync<T>(string exchange) where T : class
        {
            await Logger.LogSuccessAsync($"Создаем клиента для {exchange}...");

            try
            {
                return exchange switch
                {
                    "binance" => await Task.Run(() => new BinanceRestClient()),
                    "bybit" => await Task.Run(() => new BybitRestClient()),
                    "okx" => await Task.Run(() => new OKXRestClient()),
                    _ => throw new Exception($"❌ Биржа {exchange} не поддерживается.")
                };
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync($"Ошибка создания клиента для {exchange}", ex);
                return null;
            }
        }
    }

}
