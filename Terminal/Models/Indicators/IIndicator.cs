using Binance.Net.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Models.Indicators
{
    /// <summary>
    /// Интерфейс индикатора, который рассчитывает значения на основе списка свечей.
    /// </summary>
    public interface IIndicator
    {
        /// <summary>
        /// Имя индикатора (например, "SMA (14)").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Рассчитывает индикатор на основе списка свечей.
        /// Возвращает словарь, где ключ — время (например, время закрытия свечи),
        /// а значение — рассчитанное значение индикатора.
        /// </summary>
        IReadOnlyDictionary<DateTime, double> Calculate(IList<IBinanceKline> klines);
    }

}
