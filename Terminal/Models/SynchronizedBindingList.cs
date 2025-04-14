using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Terminal.Models
{
    public class SynchronizedBindingList<T> : BindingList<T>
    {
        private readonly SynchronizationContext _synchronizationContext;

        public SynchronizedBindingList()
        {
            // Сохраняем текущий SynchronizationContext
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public SynchronizedBindingList(IList<T> list) : base(list)
        {
            // Сохраняем текущий SynchronizationContext
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        // Переопределяем метод для добавления элемента с синхронизацией
        protected override void InsertItem(int index, T item)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                base.InsertItem(index, item);
            }
            else
            {
                _synchronizationContext.Send(_ => base.InsertItem(index, item), null);
            }
        }

        // Переопределяем метод для удаления элемента с синхронизацией
        protected override void RemoveItem(int index)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                base.RemoveItem(index);
            }
            else
            {
                _synchronizationContext.Send(_ => base.RemoveItem(index), null);
            }
        }

        // Переопределяем метод для замены элемента с синхронизацией
        protected override void SetItem(int index, T item)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                base.SetItem(index, item);
            }
            else
            {
                _synchronizationContext.Send(_ => base.SetItem(index, item), null);
            }
        }

        // Переопределяем метод для очистки списка с синхронизацией
        protected override void ClearItems()
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                base.ClearItems();
            }
            else
            {
                _synchronizationContext.Send(_ => base.ClearItems(), null);
            }
        }

        // Переопределяем метод для добавления диапазона элементов с синхронизацией
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            if (SynchronizationContext.Current == _synchronizationContext)
            {
                foreach (var item in items)
                {
                    Add(item);
                }
            }
            else
            {
                _synchronizationContext.Send(_ =>
                {
                    foreach (var item in items)
                    {
                        Add(item);
                    }
                }, null);
            }
        }
    }
}