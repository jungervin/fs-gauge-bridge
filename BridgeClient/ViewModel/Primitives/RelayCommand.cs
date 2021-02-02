using System;
using System.Windows.Input;

namespace BridgeClient
{
    internal class RelayCommand : ICommand
    {
        private Action _p;

        public RelayCommand(Action p)
        {
            _p = p;
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _p.Invoke();

    }
}