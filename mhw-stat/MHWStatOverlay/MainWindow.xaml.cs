using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using MHWStatOverlay.Services;
using MHWStatOverlay.ViewModels;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MHWStatOverlay;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly OverlayWindow _overlayWindow;
    private readonly UpdateLoop _updateLoop;

    public SettingsViewModel SettingsVM { get; }

    private string _connectionStatus = "";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set { _connectionStatus = value; OnPropChanged(); }
    }

    private Brush _connectionBrush = Brushes.Gray;
    public Brush ConnectionBrush
    {
        get => _connectionBrush;
        set { _connectionBrush = value; OnPropChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public MainWindow(SettingsService settings, UpdateLoop updateLoop,
                      OverlayViewModel overlayVm, OverlayWindow overlayWindow)
    {
        _overlayWindow = overlayWindow;
        _updateLoop = updateLoop;
        _connectionStatus = L.Get("SearchingMHW");

        SettingsVM = new SettingsViewModel(settings);

        InitializeComponent();
        DataContext = this;

        ApplyLocalization();

        updateLoop.ConnectionChanged += connected =>
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionBrush = connected ? Brushes.LimeGreen : Brushes.Gray;
            });
        };

        updateLoop.StatusMessage += message =>
        {
            Dispatcher.Invoke(() => ConnectionStatus = message);
        };

        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    private void ApplyLocalization()
    {
        TitleText.Text = L.Get("AppName");
        BtnSave.Content = L.Get("Save");
        BtnReset.Content = L.Get("Reset");

        TabGeneral.Header = L.Get("TabGeneral");
        TabMonster.Header = L.Get("TabMonster");
        TabDamage.Header = L.Get("TabDamage");
        TabQuest.Header = L.Get("TabQuest");

        GrpGeneral.Header = L.Get("GeneralSettings");
        LblPolling.Text = L.Get("PollingRate");
        LblOpacity.Text = L.Get("Opacity");
        ChkLocked.Content = L.Get("OverlayLocked");

        GrpMonster.Header = L.Get("MonsterHPWidget");
        ChkShowMonster.Content = L.Get("ShowMonsterWidget");
        ChkShowHP.Content = L.Get("ShowHPBar");
        ChkShowHPValues.Content = L.Get("ShowHPValues");
        LblMonsterWidth.Text = L.Get("Width");

        GrpPart.Header = L.Get("PartWidget");
        ChkShowPart.Content = L.Get("ShowPartWidget");
        ChkShowBreak.Content = L.Get("ShowBreakCount");
        LblPartWidth.Text = L.Get("Width");

        GrpDamage.Header = L.Get("DamageWidget");
        ChkShowDamage.Content = L.Get("ShowDamageWidget");
        ChkShowDmgNumbers.Content = L.Get("ShowDamageNumbers");
        ChkShowPct.Content = L.Get("ShowPercentage");
        LblDamageWidth.Text = L.Get("Width");

        GrpQuest.Header = L.Get("QuestWidget");
        ChkShowQuest.Content = L.Get("ShowQuestWidget");
        LblQuestWidth.Text = L.Get("Width");
    }

    public void ShowAndActivate()
    {
        Show();
        Activate();
    }
}
