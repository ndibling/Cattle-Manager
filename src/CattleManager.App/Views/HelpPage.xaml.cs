using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace CattleManager.App.Views;

public partial class HelpPage : Page
{
    public HelpPage()
    {
        InitializeComponent();

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = v is not null ? $"v{v.Major}.{v.Minor}.{v.Build}" : "Unknown";

        TxtDataPath.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CattleManager");
    }
}
