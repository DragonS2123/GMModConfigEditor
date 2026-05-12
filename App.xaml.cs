using System;
using System.Windows;
using System.Windows.Threading;

namespace GMCraftTableEditor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ловим необработанные исключения в UI-потоке
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                ex.Exception.ToString(),
                "Необработанная ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        // Ловим исключения в фоновых потоках
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                ex.ExceptionObject?.ToString() ?? "Неизвестная ошибка",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        try
        {
            // Применяем тёмную тему (цвета уже в App.xaml, это просто синхронизирует ThemeManager)
            ThemeManager.Apply(dark: true);

            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка запуска GMCraftTableEditor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
