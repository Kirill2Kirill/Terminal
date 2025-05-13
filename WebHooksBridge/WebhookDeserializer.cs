using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    public static class WebhookDeserializer
    {
        public static async Task<TradingSignal?> DeserializeAsync(string json)
        {
            try
            {
                var signal = JsonConvert.DeserializeObject<TradingSignal>(json);
                if (signal == null || string.IsNullOrEmpty(signal.Symbol) || signal.Exchanges.Length == 0)
                {
                    await Logger.LogErrorAsync("❌ Ошибка десериализации: некорректные данные вебхука.");
                    return null;
                }

                await Logger.LogSuccessAsync($"✅ Вебхук успешно десериализован: {signal.Symbol}");
                return signal;
            }
            catch (Exception ex)
            {
                await Logger.LogErrorAsync("❌ Ошибка при десериализации вебхука", ex);
                return null;
            }
        }
    }
}
