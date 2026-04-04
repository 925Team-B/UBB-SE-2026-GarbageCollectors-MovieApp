using MovieApp.UI.ViewModels;
using Xunit;

namespace Tests.Unit.ViewModels;

public class RelayCommandTests
{

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenExecuteIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }


    [Fact]
    public void CanExecute_ReturnsTrue_WhenNoPredicateProvided()
    {
        var command = new RelayCommand(_ => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsTrue_WhenPredicateReturnsTrue()
    {
        var command = new RelayCommand(_ => { }, _ => true);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReturnsFalse_WhenPredicateReturnsFalse()
    {
        var command = new RelayCommand(_ => { }, _ => false);

        Assert.False(command.CanExecute(null));
    }


    [Fact]
    public void Execute_CallsTheProvidedAction()
    {
        bool wasCalled = false;
        var command = new RelayCommand(_ => wasCalled = true);

        command.Execute(null);

        Assert.True(wasCalled);
    }

    [Fact]
    public void Execute_PassesParameterToAction()
    {
        object? received = null;
        var command = new RelayCommand(p => received = p);

        command.Execute("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void Execute_DoesNotRun_WhenCanExecuteReturnsFalse()
    {
        bool wasCalled = false;
        var command = new RelayCommand(_ => wasCalled = true, _ => false);

        if (command.CanExecute(null))
            command.Execute(null);

        Assert.False(wasCalled);
    }

    [Fact]
    public void RaiseCanExecuteChanged_FiresCanExecuteChangedEvent()
    {
        var command = new RelayCommand(_ => { });
        bool eventFired = false;
        command.CanExecuteChanged += (_, _) => eventFired = true;

        command.RaiseCanExecuteChanged();

        Assert.True(eventFired);
    }

    [Fact]
    public void RaiseCanExecuteChanged_SendsSenderAsCommand()
    {
        var command = new RelayCommand(_ => { });
        object? receivedSender = null;
        command.CanExecuteChanged += (s, _) => receivedSender = s;

        command.RaiseCanExecuteChanged();

        Assert.Equal(command, receivedSender);
    }

    [Fact]
    public void RaiseCanExecuteChanged_DoesNotThrow_WhenNoHandlersAttached()
    {
        var command = new RelayCommand(_ => { });

        var ex = Record.Exception(() => command.RaiseCanExecuteChanged());

        Assert.Null(ex);
    }
}
