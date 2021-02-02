using BridgeClient.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace BridgeClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var simConnectViewModel = new SimConnectViewModel();
            var mainWindowViewModel = new MainWindowViewModel(simConnectViewModel);

            mainWindowViewModel.Input = "(A:LIGHT LANDING,Bool)";

            var window = new MainWindow { DataContext = mainWindowViewModel };
            window.Show();
        }
    }
}
