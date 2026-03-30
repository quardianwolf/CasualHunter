using System.Windows.Input;
using MHWStatOverlay.Services;

namespace MHWStatOverlay.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    public OverlaySettings Settings => _settingsService.Settings;

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    // General
    public int PollingRateMs
    {
        get => Settings.PollingRateMs;
        set { Settings.PollingRateMs = value; OnPropertyChanged(); }
    }
    public bool OverlayLocked
    {
        get => Settings.OverlayLocked;
        set { Settings.OverlayLocked = value; OnPropertyChanged(); }
    }
    public double GlobalOpacity
    {
        get => Settings.GlobalOpacity;
        set { Settings.GlobalOpacity = value; OnPropertyChanged(); }
    }

    // Monster
    public bool ShowMonsterWidget
    {
        get => Settings.ShowMonsterWidget;
        set { Settings.ShowMonsterWidget = value; OnPropertyChanged(); }
    }
    public bool ShowMonsterHP
    {
        get => Settings.ShowMonsterHP;
        set { Settings.ShowMonsterHP = value; OnPropertyChanged(); }
    }
    public bool ShowMonsterHPValues
    {
        get => Settings.ShowMonsterHPValues;
        set { Settings.ShowMonsterHPValues = value; OnPropertyChanged(); }
    }
    public double MonsterWidgetWidth
    {
        get => Settings.MonsterWidgetWidth;
        set { Settings.MonsterWidgetWidth = value; OnPropertyChanged(); }
    }

    // Parts
    public bool ShowPartWidget
    {
        get => Settings.ShowPartWidget;
        set { Settings.ShowPartWidget = value; OnPropertyChanged(); }
    }
    public bool ShowPartBreakCount
    {
        get => Settings.ShowPartBreakCount;
        set { Settings.ShowPartBreakCount = value; OnPropertyChanged(); }
    }
    public double PartWidgetWidth
    {
        get => Settings.PartWidgetWidth;
        set { Settings.PartWidgetWidth = value; OnPropertyChanged(); }
    }

    // Damage
    public bool ShowDamageWidget
    {
        get => Settings.ShowDamageWidget;
        set { Settings.ShowDamageWidget = value; OnPropertyChanged(); }
    }
    public bool ShowDamageNumbers
    {
        get => Settings.ShowDamageNumbers;
        set { Settings.ShowDamageNumbers = value; OnPropertyChanged(); }
    }
    public bool ShowDamagePercentage
    {
        get => Settings.ShowDamagePercentage;
        set { Settings.ShowDamagePercentage = value; OnPropertyChanged(); }
    }
    public double DamageWidgetWidth
    {
        get => Settings.DamageWidgetWidth;
        set { Settings.DamageWidgetWidth = value; OnPropertyChanged(); }
    }

    // Quest
    public bool ShowQuestWidget
    {
        get => Settings.ShowQuestWidget;
        set { Settings.ShowQuestWidget = value; OnPropertyChanged(); }
    }
    public double QuestWidgetWidth
    {
        get => Settings.QuestWidgetWidth;
        set { Settings.QuestWidgetWidth = value; OnPropertyChanged(); }
    }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        SaveCommand = new RelayCommand(() => _settingsService.Save());
        ResetCommand = new RelayCommand(() =>
        {
            _settingsService.Settings = new OverlaySettings();
            _settingsService.Save();
            OnPropertyChanged("");
        });

        // Any property change should immediately notify overlay
        PropertyChanged += (_, _) => _settingsService.NotifyChanged();
    }
}
