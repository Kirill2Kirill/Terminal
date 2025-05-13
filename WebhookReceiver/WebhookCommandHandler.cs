using Binance.Net.Clients;
using OKX.Net.Objects.Public;

namespace WebhookReceiver
{
    public class WebhookCommandHandler
    {
        public WebhookCommandHandler(List<User> users)
        {
            Users = users;
        }

        public List<User> Users { get; set; }

        public async Task UpdateUsersAsync()
        {
            Users = await UserStorage.ReadUsersAsync();
        }

        public async Task HandleAsync(TradingViewSignal signal)
        {
            var user = Users.Find(user => user.Name == signal.User);

            if (user == null)
            {
                Logger.Error($"Пользователь с ключом {signal.HookKey} не найден.");
                return;
            }

            await HandleExcnhageAsync(user, signal);

        }

        public async Task HandleExcnhageAsync(User user, TradingViewSignal signal)
        {
            var tasks = signal.Exchanges.Select(async exchange =>
            {
                await HandleActionsAsync(user, exchange, signal);
            });

            await Task.WhenAll(tasks);
        }

        public async Task HandleActionsAsync(User user, SignalExchange exchange, TradingViewSignal signal)
        { 
        
        }

        public async Task PlaceOrder()
        {
            var binanceClient = new BinanceRestClient();

        //    binanceClient.UsdFuturesApi.Trading.CancelMultipleOrdersAsync(
        //        symbol: "BTCUSDT",
        //        orderClientIds: new List<string> { "order1", "order2" }
        //    );

        //    binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync();

        //    binanceClient.UsdFuturesApi.Trading.PlaceMultipleOrdersAsync();



        //    await binanceClient.SpotApi.Trading.PlaceOrderAsync(
        //        symbol,
        //        order.Action == OrderAction.Buy ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell,
        //        Binance.Net.Enums.OrderType.Limit,
        //        order.Amount,
        //        price: order.Price,
        //        clientOrderId: order.ClientId
        //    );
        //}

        //Console.WriteLine($"📝 Обработка сигнала: Action={payload.Action}, Symbol={payload.Symbol}");

        //switch (payload.Action?.ToLowerInvariant())
        //{
        //    case "buy":
        //        await ExecuteBuyAsync(payload);
        //        break;

        //    case "sell":
        //        await ExecuteSellAsync(payload);
        //        break;

        //    default:
        //        Console.WriteLine("⚠️ Неизвестная команда: " + payload.Action);
        //        break;
        //}
    }

        //private Task ExecuteBuyAsync(TradingViewWebhookPayload payload)
        //{
        //    Console.WriteLine($"📈 Покупка {payload.Symbol} по цене {payload.Price}");
        //    // TODO: добавить реальную логику (бот, логгинг, API вызов и т.д.)
        //    return Task.CompletedTask;
        //}

        //private Task ExecuteSellAsync(TradingViewWebhookPayload payload)
        //{
        //    Console.WriteLine($"📉 Продажа {payload.Symbol} по цене {payload.Price}");
        //    // TODO: добавить реальную логику
        //    return Task.CompletedTask;
        //}
    }
}
