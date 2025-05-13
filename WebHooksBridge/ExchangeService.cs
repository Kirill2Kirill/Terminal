using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    public static class ExchangeService
    {
        public static async Task ProcessTradingSignalAsync(TradingSignal signal)
        {
            var tasks = signal.Exchanges.Select(async exchange =>
            {
                await Logger.LogSuccessAsync($"Обрабатываем сигнал для {exchange}");
                await OrderManager.ProcessOrdersAsync(exchange, signal);
            });

            await Task.WhenAll(tasks);
        }
    }

}
