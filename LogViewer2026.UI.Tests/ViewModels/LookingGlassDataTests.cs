using FluentAssertions;
using LogViewer2026.UI.ViewModels;

namespace LogViewer2026.UI.Tests.ViewModels;

public class LookingGlassDataTests
{
    [Fact]
    public void DefaultValues_ShouldBeEmpty()
    {
        var data = new MainViewModel.LookingGlassData();

        data.Text.Should().BeEmpty();
        data.HighlightStartOffset.Should().Be(-1);
        data.HighlightLength.Should().Be(0);
        data.StartingLineNumber.Should().Be(1);
    }

    [Fact]
    public void SetText_ShouldRaisePropertyChanged()
    {
        var data = new MainViewModel.LookingGlassData();
        var propertyChanged = false;
        data.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.LookingGlassData.Text))
                propertyChanged = true;
        };

        data.Text = "new text";

        propertyChanged.Should().BeTrue();
        data.Text.Should().Be("new text");
    }

    [Fact]
    public void SetHighlightStartOffset_ShouldRaisePropertyChanged()
    {
        var data = new MainViewModel.LookingGlassData();
        var propertyChanged = false;
        data.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.LookingGlassData.HighlightStartOffset))
                propertyChanged = true;
        };

        data.HighlightStartOffset = 42;

        propertyChanged.Should().BeTrue();
        data.HighlightStartOffset.Should().Be(42);
    }

    [Fact]
    public void SetHighlightLength_ShouldRaisePropertyChanged()
    {
        var data = new MainViewModel.LookingGlassData();
        var propertyChanged = false;
        data.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.LookingGlassData.HighlightLength))
                propertyChanged = true;
        };

        data.HighlightLength = 10;

        propertyChanged.Should().BeTrue();
        data.HighlightLength.Should().Be(10);
    }

    [Fact]
    public void SetStartingLineNumber_ShouldRaisePropertyChanged()
    {
        var data = new MainViewModel.LookingGlassData();
        var propertyChanged = false;
        data.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.LookingGlassData.StartingLineNumber))
                propertyChanged = true;
        };

        data.StartingLineNumber = 25;

        propertyChanged.Should().BeTrue();
        data.StartingLineNumber.Should().Be(25);
    }

    [Fact]
    public void SetSameValue_ShouldNotRaisePropertyChanged()
    {
        var data = new MainViewModel.LookingGlassData();
        data.Text = "initial";
        var propertyChanged = false;
        data.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.LookingGlassData.Text))
                propertyChanged = true;
        };

        data.Text = "initial";

        propertyChanged.Should().BeFalse();
    }
}
