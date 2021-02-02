using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BridgeClient
{
    internal class BaseViewModel : INotifyPropertyChanged
    {
        private Dictionary<string, object> _values = new Dictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected T Get<T>([CallerMemberName] string name = null)
        {
            return _values.ContainsKey(name) ? (T)_values[name] : default(T);
        }

        protected void Set<T>(T value, [CallerMemberName]string name = null)
        {
            if (!_values.ContainsKey(name) || !EqualityComparer<T>.Default.Equals((T)_values[name], value))
            {
                _values[name] = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                OnPropertyChanged(name);
            }
            else
            {
                _values[name] = value;

            }
        }

        protected virtual void OnPropertyChanged(string name)
        {

        }
    }
}