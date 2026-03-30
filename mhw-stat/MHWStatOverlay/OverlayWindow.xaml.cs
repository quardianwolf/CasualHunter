using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MHWStatOverlay.Helpers;
using MHWStatOverlay.Services;
using MHWStatOverlay.ViewModels;

namespace MHWStatOverlay;

public partial class OverlayWindow : Window
{
    private readonly DispatcherTimer _topmostTimer;
    private readonly SettingsService _settings;
    private bool _editMode;
    private UIElement? _dragTarget;
    private System.Windows.Point _dragStart;
    private double _dragOrigLeft;
    private double _dragOrigTop;
    private bool _insertHeld;
    private bool _escHeld;
    private DispatcherTimer? _escHintTimer;

    public OverlayWindow(OverlayViewModel viewModel, SettingsService settings)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settings = settings;
        Opacity = settings.Settings.GlobalOpacity;

        settings.SettingsChanged += () =>
        {
            Dispatcher.Invoke(() => Opacity = settings.Settings.GlobalOpacity);
        };

        Loaded += (_, _) =>
        {
            WindowHelper.SetClickThrough(this);
            WindowHelper.HideFromTaskbar(this);
            WindowHelper.SetTopmost(this);
        };

        _topmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _topmostTimer.Tick += (_, _) => WindowHelper.SetTopmost(this);
        _topmostTimer.Start();

        // Poll Insert key for edit mode toggle
        var keyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        keyTimer.Tick += (_, _) =>
        {
            bool insertDown = (Keyboard.GetKeyStates(Key.Insert) & KeyStates.Down) != 0;
            if (insertDown && !_insertHeld)
            {
                _insertHeld = true;
                ToggleEditMode();
            }
            else if (!insertDown)
            {
                _insertHeld = false;
            }

            // Show hint when ESC is pressed (game menu opens)
            bool escDown = (Keyboard.GetKeyStates(Key.Escape) & KeyStates.Down) != 0;
            if (escDown && !_escHeld && !_editMode)
            {
                _escHeld = true;
                ShowEscHint();
            }
            else if (!escDown)
            {
                _escHeld = false;
            }
        };
        keyTimer.Start();
    }

    private void ShowEscHint()
    {
        EscHint.Visibility = Visibility.Visible;
        _escHintTimer?.Stop();
        _escHintTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _escHintTimer.Tick += (_, _) =>
        {
            EscHint.Visibility = Visibility.Collapsed;
            _escHintTimer.Stop();
        };
        _escHintTimer.Start();
    }

    private void ToggleEditMode()
    {
        _editMode = !_editMode;
        SetEditMode(_editMode);
    }

    public void SetEditMode(bool enabled)
    {
        _editMode = enabled;

        if (enabled)
        {
            WindowHelper.RemoveClickThrough(this);
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x20, 0x00, 0x00, 0x00));
            EditToolbar.Visibility = Visibility.Visible;
            UpdateToggleButtons();

            foreach (UIElement child in OverlayCanvas.Children)
            {
                if (child is FrameworkElement fe && fe.Tag is not "status")
                {
                    fe.MouseLeftButtonDown += Widget_MouseDown;
                    fe.MouseMove += Widget_MouseMove;
                    fe.MouseLeftButtonUp += Widget_MouseUp;
                    fe.Cursor = System.Windows.Input.Cursors.SizeAll;
                }
            }
        }
        else
        {
            WindowHelper.SetClickThrough(this);
            Background = System.Windows.Media.Brushes.Transparent;
            EditToolbar.Visibility = Visibility.Collapsed;

            foreach (UIElement child in OverlayCanvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    fe.MouseLeftButtonDown -= Widget_MouseDown;
                    fe.MouseMove -= Widget_MouseMove;
                    fe.MouseLeftButtonUp -= Widget_MouseUp;
                    fe.Cursor = null;
                }
            }

            _settings.Save();
        }
    }

    private void ShowAllParts_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is OverlayViewModel vm)
            vm.ShowAllParts();
    }

    private void ToggleWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        var s = _settings.Settings;

        switch (btn.Tag?.ToString())
        {
            case "Monster": s.ShowMonsterWidget = !s.ShowMonsterWidget; break;
            case "Parts": s.ShowPartWidget = !s.ShowPartWidget; break;
            case "Damage": s.ShowDamageWidget = !s.ShowDamageWidget; break;
            case "Quest": s.ShowQuestWidget = !s.ShowQuestWidget; break;
        }

        UpdateToggleButtons();
        // Force binding update
        if (DataContext is OverlayViewModel vm)
        {
            vm.NotifySettingsChanged();
        }
    }

    private void UpdateToggleButtons()
    {
        var s = _settings.Settings;
        var on = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x60, 0x48, 0xC8, 0x48));
        var off = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x30, 0xFF, 0x50, 0x50));

        BtnToggleMonster.Background = s.ShowMonsterWidget ? on : off;
        BtnToggleParts.Background = s.ShowPartWidget ? on : off;
        BtnToggleDamage.Background = s.ShowDamageWidget ? on : off;
        BtnToggleQuest.Background = s.ShowQuestWidget ? on : off;
    }

    private void Widget_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement el)
        {
            _dragTarget = el;
            _dragStart = e.GetPosition(OverlayCanvas);
            _dragOrigLeft = Canvas.GetLeft(el);
            _dragOrigTop = Canvas.GetTop(el);
            if (double.IsNaN(_dragOrigLeft)) _dragOrigLeft = 0;
            if (double.IsNaN(_dragOrigTop)) _dragOrigTop = 0;
            el.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragTarget != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(OverlayCanvas);
            double newLeft = _dragOrigLeft + (pos.X - _dragStart.X);
            double newTop = _dragOrigTop + (pos.Y - _dragStart.Y);

            Canvas.SetLeft(_dragTarget, newLeft);
            Canvas.SetTop(_dragTarget, newTop);
            UpdateWidgetPosition(_dragTarget, newLeft, newTop);
        }
    }

    private void Widget_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragTarget?.ReleaseMouseCapture();
        _dragTarget = null;
    }

    private void UpdateWidgetPosition(UIElement widget, double x, double y)
    {
        var s = _settings.Settings;
        var name = (widget as FrameworkElement)?.Name;

        switch (name)
        {
            case "MonsterWidgetCtrl":
                s.MonsterWidgetX = x; s.MonsterWidgetY = y; break;
            case "PartWidgetCtrl":
                s.PartWidgetX = x; s.PartWidgetY = y; break;
            case "DamageWidgetCtrl":
                s.DamageWidgetX = x; s.DamageWidgetY = y; break;
            case "QuestWidgetCtrl":
                s.QuestWidgetX = x; s.QuestWidgetY = y; break;
        }
    }
}
