using System.Windows.Controls;

namespace CattleManager.App.Services;

public class NavigationService
{
    private Frame? _frame;
    private readonly Stack<Page> _backStack = new();

    public void Initialize(Frame frame) => _frame = frame;

    public void NavigateTo(Page page)
    {
        if (_frame?.Content is Page current)
            _backStack.Push(current);
        _frame!.Navigate(page);
    }

    public void GoBack()
    {
        if (_backStack.Count > 0)
            _frame!.Navigate(_backStack.Pop());
        else if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    public bool CanGoBack => _backStack.Count > 0 || (_frame?.CanGoBack ?? false);

    public void ClearBack() => _backStack.Clear();
}
