using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using MHWStatOverlay.Services;
using MHWStatOverlay.ViewModels;
using Application = System.Windows.Application;

namespace MHWStatOverlay;

public partial class App : Application
{
    private NotifyIcon? _trayIcon;
    private UpdateLoop? _updateLoop;
    private MainWindow? _mainWindow;
    private OverlayWindow? _overlayWindow;

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        L.Init();

        var settings = new SettingsService();
        settings.Load();

        _updateLoop = new UpdateLoop(settings);
        var overlayVm = new OverlayViewModel(_updateLoop, settings);

        _overlayWindow = new OverlayWindow(overlayVm, settings);
        _overlayWindow.Show();

        _mainWindow = new MainWindow(settings, _updateLoop, overlayVm, _overlayWindow);

        SetupTrayIcon();

        _updateLoop.Start();
        _mainWindow.ShowAndActivate();
    }

    private void SetupTrayIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
        var icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;

        _trayIcon = new NotifyIcon
        {
            Text = L.Get("AppName"),
            Icon = icon,
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(L.Get("Settings"), null, (_, _) => _mainWindow?.ShowAndActivate());
        menu.Items.Add("-");
        menu.Items.Add(L.Get("Exit"), null, (_, _) =>
        {
            _updateLoop?.Dispose();
            _trayIcon!.Visible = false;
            _trayIcon.Dispose();
            Shutdown();
        });

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => _mainWindow?.ShowAndActivate();
    }
}
