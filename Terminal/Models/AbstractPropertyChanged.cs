using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Models
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class AbstractPropertyChanged : INotifyPropertyChanged
    {
        public SynchronizationContext? _ctx = SynchronizationContext.Current;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            if (_ctx != null)
                _ctx.Post(_ => PropertyChanged?.Invoke(this, eventArgs), null);
            else
                PropertyChanged?.Invoke(this, eventArgs);
        }

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (_ctx != null)
                _ctx.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)), null);
            else
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
