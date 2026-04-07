#nullable enable
using System.Windows.Input;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// A synchronous implementation of ICommand using the relay/delegate pattern.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Func<object?, bool>? canExecute;

    /// <summary>
    /// Initializes a new instance of <see cref="RelayCommand"/>.
    /// </summary>
    /// <param name="execute">The action to execute.</param>
    /// <param name="canExecute">Optional predicate for command availability.</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <summary>Occurs when CanExecute state changes.</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Determines whether the command can execute.</summary>
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    /// <summary>Executes the command action.</summary>
    public void Execute(object? parameter) => execute(parameter);

    /// <summary>Raises CanExecuteChanged to re-evaluate command state.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// An async implementation of ICommand using the relay/delegate pattern.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> execute;
    private readonly Func<object?, bool>? canExecute;
    private bool isExecuting;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncRelayCommand"/>.
    /// </summary>
    /// <param name="execute">The async action to execute.</param>
    /// <param name="canExecute">Optional predicate for command availability.</param>
    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <summary>Occurs when CanExecute state changes.</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Determines whether the command can execute.</summary>
    public bool CanExecute(object? parameter) => !isExecuting && (canExecute?.Invoke(parameter) ?? true);

    /// <summary>Executes the async command action.</summary>
    public async void Execute(object? parameter)
    {
        if (isExecuting) return;

        isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await execute(parameter);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand ERROR] {ex}");
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Raises CanExecuteChanged to re-evaluate command state.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
