
using Binance.Net.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Models.Indicators
{
    /// <summary>
    /// Рассчитывает экспоненциальную скользящую среднюю (EMA).
    /// </summary>
    public class EMA : IIndicator
    {
        public string Name { get; }
        public int Period { get; }

        public EMAIndicator(int period)
        {
            if (period <= 0)
                throw new ArgumentException("Период должен быть больше 0.", nameof(period));
            Period = period;
            Name = $"EMA ({period})";
        }

        public IReadOnlyDictionary<DateTime, double> Calculate(IList<IBinanceKline> klines)
        {
            var result = new Dictionary<DateTime, double>();
            if (klines == null || klines.Count < Period)
                return result;

            // Начальное значение EMA = SMA первого окна
            decimal sum = 0;
            for (int i = 0; i < Period; i++)
            {
                sum += klines[i].ClosePrice;
            }
            double previousEMA = (double)(sum / Period);
            result[klines[Period - 1].OpenTime] = previousEMA;
            double multiplier = 2d / (Period + 1);

            for (int i = Period; i < klines.Count; i++)
            {
                double currentClose = (double)klines[i].ClosePrice;
                double currentEMA = ((currentClose - previousEMA) * multiplier) + previousEMA;
                result[klines[i].OpenTime] = currentEMA;
                previousEMA = currentEMA;
            }
            return result;
        }
    }

}
