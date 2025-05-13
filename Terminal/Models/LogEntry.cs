using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Models
{
    // Модель для записи лога
    public class LogEntry
    {
        public int Index { get; set; } // Индекс записи
        public string Level { get; set; } // Уровень (Message, Error, Ok)
        public string Text { get; set; }  // Текст лога
        public DateTime Timestamp { get; set; } // Время записи
    }

}
