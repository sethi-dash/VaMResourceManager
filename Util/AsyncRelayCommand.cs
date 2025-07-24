using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Vrm.Util
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = _ => execute() ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = _ =>
            {
                if (canExecute == null)
                    return true;
                return canExecute();
            };
        }

        public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute = null)
        {
            _execute = x => execute(x) ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = x =>
            {
                if (canExecute == null)
                    return true;
                return canExecute(x);
            };
        }

        public bool CanExecute(object parameter) => !_isExecuting && _canExecute(parameter);

        public async void Execute(object parameter)
        {
            ExecutionTask = ExecuteAsync(parameter);
            await ExecutionTask;
        }

        private async Task ExecuteAsync(object parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public Task ExecutionTask { get; private set; }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                _canExecuteChangedHandlers += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                _canExecuteChangedHandlers -= value;
            }
        }

        private EventHandler _canExecuteChangedHandlers;

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChangedHandlers?.Invoke(this, EventArgs.Empty);
        }
    }
}
