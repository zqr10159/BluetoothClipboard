using System.Configuration;
using System.Data;
using System.Windows;
using Serilog;

namespace BluetoothClipboard;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 配置Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/bluetooth_clipboard.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
            
        Log.Information("应用程序启动");
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("应用程序关闭");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

