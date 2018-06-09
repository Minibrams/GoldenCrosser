using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoldenCrosser
{
    public class RelayCommand : ICommand
    {
        private readonly Predicate<object> _canExecutePredicate;

        private readonly Action<object> _executeAction;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> executeAction, Predicate<object> canExecutePredicate = null)
        {
            _executeAction = executeAction;
            _canExecutePredicate = canExecutePredicate;
        }

        public bool CanExecute(object parameter) => _canExecutePredicate?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _executeAction?.Invoke(parameter);

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);


    }
}
