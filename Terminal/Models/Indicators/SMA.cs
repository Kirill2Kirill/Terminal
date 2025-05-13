using Binance.Net.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Models.Indicators
{
    /// <summary>
    /// Рассчитывает простую скользящую среднюю (SMA) для закрывающих цен.
    /// </summary>
    public class SMA : IIndicator
    {
        public string Name { get; }
        public int Period { get; }

        public SMA(int period)
        {
            if (period <= 0)
                throw new ArgumentException("Период должен быть больше 0.", nameof(period));
            Period = period;
            Name = $"SMA ({period})";
        }

        public IReadOnlyDictionary<DateTime, double> Calculate(IList<IBinanceKline> klines)
        {
            var result = new Dictionary<DateTime, double>();
            if (klines == null || klines.Count < Period)
                return result;

            // Считаем SMA для каждого окна из Period свечей.
            for (int i = Period - 1; i < klines.Count; i++)
            {
                decimal sum = 0;
                for (int j = i - Period + 1; j <= i; j++)
                {
                    sum += klines[j].ClosePrice;
                }
                double average = (double)(sum / Period);
                // Здесь ключом может быть время последней свечи окна
                result[klines[i].OpenTime] = average;
            }
            return result;
        }
    }

}
