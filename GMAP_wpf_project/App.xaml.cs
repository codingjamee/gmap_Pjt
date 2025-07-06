using System.Configuration;
using System.Data;
using System.Windows;

namespace GMAP_wpf_project
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 전역 예외 처리
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"예상치 못한 오류가 발생했습니다:\n{args.Exception.Message}",
                               "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }

}
