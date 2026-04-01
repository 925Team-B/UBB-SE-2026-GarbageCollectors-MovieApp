#nullable enable
using System.Windows.Input;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// A synchronous implementation of ICommand using the relay/delegate pattern.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of <see cref="RelayCommand"/>.
    /// </summary>
    /// <param name="execute">The action to execute.</param>
    /// <param name="canExecute">Optional predicate for command availability.</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>Occurs when CanExecute state changes.</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Determines whether the command can execute.</summary>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>Executes the command action.</summary>
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>Raises CanExecuteChanged to re-evaluate command state.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// An async implementation of ICommand using the relay/delegate pattern.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncRelayCommand"/>.
    /// </summary>
    /// <param name="execute">The async action to execute.</param>
    /// <param name="canExecute">Optional predicate for command availability.</param>
    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>Occurs when CanExecute state changes.</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Determines whether the command can execute.</summary>
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    /// <summary>Executes the async command action.</summary>
    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand ERROR] {ex}");
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Raises CanExecuteChanged to re-evaluate command state.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
