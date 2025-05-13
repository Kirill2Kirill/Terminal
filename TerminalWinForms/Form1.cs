using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using LiveChartsCore.Measure;
using Terminal.Models;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using Terminal.Managers;

namespace TerminalWinForms
{
    public partial class Form1 : Form
    {
        private BinanceHistoryFilesProcessor binanceHistoryFilesProcessor;
        private List<HistoryKline> myKlines;
        Button button;

        public Form1()
        {
            InitializeComponent();

            binanceHistoryFilesProcessor = new BinanceHistoryFilesProcessor(Terminal.Enums.BinanceMarketType.Futures);

            // Добавляем кнопку для вызова ChartForm
            button = new Button
            {
                Text = "Открыть график",
                Dock = DockStyle.Fill
            };
            button.Enabled = false;
            button.Click += OpenChartForm;
            Controls.Add(button);

        }

        private void OpenChartForm(object sender, EventArgs e)
        {
            // Пример данных MyKline
            //var klines = new List<MyKline>
            //{
            //    new MyKline { OpenTime = DateTime.Now.AddMinutes(-30), OpenPrice = 100, HighPrice = 110, LowPrice = 95, ClosePrice = 105 },
            //    new MyKline { OpenTime = DateTime.Now.AddMinutes(-29), OpenPrice = 105, HighPrice = 115, LowPrice = 100, ClosePrice = 110 },
            //    new MyKline { OpenTime = DateTime.Now.AddMinutes(-28), OpenPrice = 110, HighPrice = 120, LowPrice = 105, ClosePrice = 115 },
            //    // Добавьте больше данных...
            //};

            //klines = binanceHistoryFilesProcessor.LoadKlinesFromPathAsync();

            // Создаем и отображаем ChartForm
            var chartForm = new ChartForm("BTC-USDT\nFUTURES", myKlines);
            //chartForm.Form = this; // Передаем ссылку на родительскую форму, если нужно
            chartForm.FormClosed += (s, args) =>
            {
                this.Close();
            };
            chartForm.Show(); // Используйте Show() для немодального окна или ShowDialog() для модального
        }
    

        private async void Form1_Load(object sender, EventArgs e)
        {
            //await binanceHistoryFilesProcessor.ProcessFilesAsync("BTCUSDT", 0, 2);
            var filePath = @"C:\Users\Kirill\source\repos\Terminal\TerminalWinForms\bin\Debug\net9.0-windows\DATA\BTCUSDT\MessagePack\BTCUSDT_all_data.msgpack";
            myKlines = await binanceHistoryFilesProcessor.LoadAndAggregateKlinesAsync(filePath, 45);

            ////myKlines = null;
            //myKlines = await binanceHistoryFilesProcessor.LoadKlinesFromPathAsync(filePath);
            myKlines = myKlines.TakeLast(300).OrderBy(k=>k.OpenTime).ToList();

            button.Enabled = true;
            //LoadKlines(klines);

        }
    }
}