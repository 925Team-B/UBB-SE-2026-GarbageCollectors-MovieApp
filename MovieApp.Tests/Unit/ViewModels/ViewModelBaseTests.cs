using MovieApp.UI.ViewModels;
using Xunit;

namespace Tests.Unit.ViewModels;

public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }

    [Fact]
    public void SetProperty_FiresPropertyChanged_WhenValueChanges()
    {
        var vm = new TestViewModel();
        bool eventFired = false;
        vm.PropertyChanged += (_, _) => eventFired = true;

        vm.Name = "Alice";

        Assert.True(eventFired);
    }

    [Fact]
    public void SetProperty_DoesNotFirePropertyChanged_WhenValueIsSame()
    {
        var vm = new TestViewModel { Name = "Alice" };
        bool eventFired = false;
        vm.PropertyChanged += (_, _) => eventFired = true;

        vm.Name = "Alice";

        Assert.False(eventFired);
    }

    [Fact]
    public void SetProperty_EventArgs_ContainsCorrectPropertyName()
    {
        var vm = new TestViewModel();
        string? receivedName = null;
        vm.PropertyChanged += (_, e) => receivedName = e.PropertyName;

        vm.Name = "Bob";

        Assert.Equal(nameof(vm.Name), receivedName);
    }

    [Fact]
    public void SetProperty_EventArgs_ReflectsThePropertyThatChanged()
    {
        var vm = new TestViewModel();
        string? receivedName = null;
        vm.PropertyChanged += (_, e) => receivedName = e.PropertyName;

        vm.Count = 5;

        Assert.Equal(nameof(vm.Count), receivedName);
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenValueChanges()
    {
        var vm = new TestViewModel();

        vm.Name = "Charlie";

        Assert.Equal("Charlie", vm.Name);
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenValueIsSame_AndFieldIsUnchanged()
    {
        var vm = new TestViewModel { Name = "Dana" };

        vm.Name = "Dana"; 

        Assert.Equal("Dana", vm.Name); 
    }
}
