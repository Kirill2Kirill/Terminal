using ScottPlot.Plottables;
using ScottPlot.WinForms;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Terminal.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using ScottPlot.TickGenerators.TimeUnits;
using ScottPlot.TickGenerators;

namespace TerminalWinForms
{
    public partial class ChartForm: Form
    {
        private readonly FormsPlot formsPlot;
        private OHLC[] ohlcData;
        private CandlestickPlot candlestickPlot;
        private string coinName;
        private Annotation annotation;
        private List<HistoryKline> klines; // Индексы совпадают с ohlcData
        private Crosshair crossHair;

        private const double Y_PADDING_PERCENT = 0.1; // 10% отступ для оси Y
        private const int DEFAULT_VISIBLE_CANDLES = 100; // Количество свечей для отображения по умолчанию

        public ChartForm(string coinName, List<HistoryKline> klines)
        {
            this.coinName = coinName;

            // Создаем график
            formsPlot = new FormsPlot { Dock = DockStyle.Fill };
            Controls.Add(formsPlot);
            this.klines = klines;

            // Преобразуем данные в OHLC формат
            if (klines != null && klines.Count != 0)
            {
                ohlcData = klines.Select(k => new OHLC(
                    open: (double)k.OpenPrice,
                    high: (double)k.HighPrice,
                    low: (double)k.LowPrice,
                    close: (double)k.ClosePrice,
                    start: k.OpenTime,
                    span: TimeSpan.FromMinutes((k.CloseTime - k.OpenTime).Minutes + 1)
                )).ToArray();
            }
            else
            {
                ohlcData = Generate.Financial.OHLCsByMinute(1000);
            }

            InitializeComponent();

            // Настраиваем график
            InitializeChart();



        }


        private void InitializeChart()
        {
            // Добавляем свечной график  
            candlestickPlot = formsPlot.Plot.Add.Candlestick(ohlcData);

            //Добавляет и настраиваем перекрестье
            crossHair = formsPlot.Plot.Add.Crosshair(0, 0);
            crossHair.LineWidth = (float)0.7; // properties set style for both lines
            crossHair.LineColor = Colors.White;
            crossHair.LinePattern = LinePattern.Dotted; // each line's styles can be individually accessed as well

            // Добавляем аннотацию  
            annotation = formsPlot.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
            annotation.IsVisible = false; // Скрываем аннотацию по умолчанию  

            // Настраиваем оси  
            formsPlot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
            formsPlot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperRight;
            var dtAx = formsPlot.Plot.Axes.DateTimeTicksBottom();

            // Настраиваем внешний вид графика
            formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#131722"); // Темный фон данных
            formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#282b30"); // Цвет основных линий сетки
            formsPlot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("#1f2227"); // Цвет второстепенных линий сетки
            formsPlot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Colors.Black;
            formsPlot.Plot.Axes.FrameColor(ScottPlot.Colors.White);

            // Настраиваем цвета свечей
            candlestickPlot.RisingColor = ScottPlot.Color.FromHex("#23ab62");// Зеленый для растущих свечей
            candlestickPlot.FallingColor = ScottPlot.Color.FromHex("#ef5350");  // Красный для падающих свечей

            // Добавляем лейбл с названием монеты  
            formsPlot.Plot.Title(coinName);

            // Показываем только последние 100 свечей изначально
            ShowLastCandles(DEFAULT_VISIBLE_CANDLES);

            //Настраиваем приближение
            CoonfigureScaling();

            // Подключаем события для отображения информации о свечах
            formsPlot.MouseMove += MouseMove_Annotation;
          
            // Обновляем график  
            formsPlot.Refresh();
        }

        private void MouseMove_Annotation(object sender, MouseEventArgs e)
        {
            Pixel mousePixel = new(e.X, e.Y);
            Coordinates mouseCoordinates = formsPlot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;

            int nearestIndex = FindNearestIndex(mouseCoordinates.X);
            if (nearestIndex >= 0 && nearestIndex < ohlcData.Length)
            {
                var nearestBar = ohlcData[nearestIndex];
                var volumeText = klines.Any() ? $"volume: {klines[nearestIndex].Volume}\n" : string.Empty;

                string annotationText = $"Индекс: {nearestIndex}\n" +
                                        $"Время: {nearestBar.DateTime:dd.MM.yyyy HH:mm}\n" +
                                        $"ТФ: {TimeSpanToTimeFrame(nearestBar.TimeSpan)}\n" +
                                        $"open: {nearestBar.Open:F2}\n" +
                                        $"close: {nearestBar.Close:F2}\n" +
                                        $"high: {nearestBar.High:F2}\n" +
                                        volumeText +
                                        $"low: {nearestBar.Low:F2}";

                if (annotation.Text != annotationText)
                {
                    annotation.Text = annotationText;
                    annotation.IsVisible = true;
                    //formsPlot.Refresh();
                }
            }
            else if (annotation.IsVisible)
            {
                annotation.IsVisible = false;
                //formsPlot.Refresh();
            }

            //обновляем всегда,чтобы отрисовывался перекрестье
            formsPlot.Refresh();
        }


        private void CoonfigureScaling()
        {
            //if (ohlcData.Any())
            //{
            //    // Ширина одной свечи
            //    //double minSpanX = ohlcData[1].DateTime.ToOADate() - ohlcData[0].DateTime.ToOADate();
            //    //минимальная позиция графика слева и справа равна ширине 10 свечей
            //    //double padding = (minSpanX * 10);
            //    //double minLeft = ohlcData.First().DateTime.ToOADate() - padding;
            //    //double maxRight = ohlcData.Last().DateTime.ToOADate() + padding;
            //    ////Работет, но пока не буду делать, т.к. непонятно
            //    //ScottPlot.AxisRules.MaximumBoundary rule = new(
            //    //     xAxis: formsPlot.Plot.Axes.Bottom,
            //    //     yAxis: formsPlot.Plot.Axes.Left,
            //    //     limits: new ScottPlot.AxisLimits(left: minLeft, right: maxRight, bottom: 0, top: 5000000));
            //    //formsPlot.Plot.Axes.Rules.Clear();
            //    //formsPlot.Plot.Axes.Rules.Add(rule);
            //}

            // enable continuous autoscaling with a custom action
            formsPlot.Plot.Axes.ContinuouslyAutoscale = true;
            formsPlot.Plot.Axes.ContinuousAutoscaleAction = (RenderPack rp) =>
            {
                AxisLimits limits = formsPlot.Plot.Axes.GetLimits();
                var xMin = limits.Left;
                var xMax = limits.Right;
                var visibleData = ohlcData.Where(c => c.DateTime.ToOADate() >= xMin && c.DateTime.ToOADate() <= xMax).ToArray();

                if (visibleData.Length == 0)
                    return;

                double yMinNew = visibleData.Min(bar => bar.Low);
                double yMaxNew = visibleData.Max(bar => bar.High);
                double paddingY = (yMaxNew - yMinNew) * Y_PADDING_PERCENT;
                yMinNew -= paddingY;
                yMaxNew += paddingY;
                rp.Plot.Axes.SetLimitsY(yMinNew, yMaxNew);

                double minSpanX = ohlcData[1].DateTime.ToOADate() - ohlcData[0].DateTime.ToOADate();
                minSpanX *= 2;
                double currentSpanX = limits.HorizontalSpan;

                if (currentSpanX < minSpanX)
                {
                    double centerX = xMin + currentSpanX / 2;
                    xMin = centerX - minSpanX / 2;
                    xMax = centerX + minSpanX / 2;
                }

                rp.Plot.Axes.SetLimitsX(xMin, xMax);
            };
        }

        private void ShowLastCandles(int visibleCandles)
        {

            // Проверяем, есть ли данные для отображения
            if (ohlcData == null || ohlcData.Length == 0)
            {
                throw new InvalidOperationException("Нет данных для отображения свечей.");
            }

            // Проверяем, достаточно ли данных для отображения
            if (ohlcData.Length < visibleCandles)
                visibleCandles = ohlcData.Length;

            // Определяем диапазон отображаемых свечей
            var visibleData = ohlcData.Skip(ohlcData.Length - visibleCandles).Take(visibleCandles);

            // Определяем границы оси X
            double startX = visibleData.First().DateTime.ToOADate();
            double endX = visibleData.Last().DateTime.ToOADate();

            // Рассчитываем полный диапазон оси X так, чтобы свечи занимали 3/4 графика
            double span = (endX - startX) / 0.75;
            double newEndX = startX + span;

            // Устанавливаем границы оси X
            formsPlot.Plot.Axes.SetLimitsX(left: startX, right: newEndX);

            // Автоматически масштабируем ось Y
            double minPrice = visibleData.Min(c => c.Low);
            double maxPrice = visibleData.Max(c => c.High);
            double padding = (maxPrice - minPrice) * Y_PADDING_PERCENT; // 10% отступ сверху и снизу
            formsPlot.Plot.Axes.SetLimitsY(bottom: minPrice - padding, top: maxPrice + padding);

            // Обновляем график
            formsPlot.Refresh();
        }
        
        private int FindNearestIndex(double mouseX)
        {
            // Проверяем, есть ли данные для поиска
            if (ohlcData == null || ohlcData.Length == 0)
            {
                throw new InvalidOperationException("Нет данных для поиска ближайшего индекса.");
            }

            // Ищем индекс ближайшего бара по X-координате
            var nearest = ohlcData
                .Select((ohlc, index) => new { Index = index, Distance = Math.Abs(ohlc.DateTime.ToOADate() - mouseX) })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (nearest == null)
            {
                throw new InvalidOperationException("Не удалось найти ближайший индекс.");
            }

            return nearest.Index;
        }
                
        private string TimeSpanToTimeFrame(TimeSpan timeSpan)
        {
            if (timeSpan.Hours > 0 && timeSpan.Minutes > 0)
            {
                return $"{timeSpan.Hours}:{timeSpan.Minutes}";
            }
            else if (timeSpan.Hours > 0)
            {
                return $"{timeSpan.Hours}H";
            }
            else if (timeSpan.Minutes > 0)
            {
                return $"{timeSpan.Minutes}MIN";
            }
            return timeSpan.ToString();
        }
    }
}
