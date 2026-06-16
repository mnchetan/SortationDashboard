using System;
using System.Windows.Input;

namespace SortationDashboard
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        // The constructor takes the method to execute, and optionally, a method to check IF it can execute
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Determines whether the command can execute in its current state.
        // WPF uses this to automatically enable/disable buttons!
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Tells WPF to re-evaluate the CanExecute state when UI interactions happen
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Executes the actual logic
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}