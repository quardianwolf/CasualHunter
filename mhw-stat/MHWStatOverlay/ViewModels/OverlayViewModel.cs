using System.Collections.ObjectModel;
using System.Windows;
using MHWStatOverlay.Models;
using MHWStatOverlay.Services;
using Application = System.Windows.Application;

namespace MHWStatOverlay.ViewModels;

public class OverlayViewModel : ViewModelBase
{
    private readonly UpdateLoop _updateLoop;
    private readonly SettingsService _settings;

    // Track hidden part names so they persist across monster data refreshes
    private readonly HashSet<string> _hiddenParts = new();
    // Track Alatreon element max (highest value seen = initial threshold)
    private float _alatreonElementMax;
    private int _alatreonLastTopple = -1;

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetField(ref _isConnected, value);
    }

    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public ObservableCollection<Monster> Monsters { get; } = new();
    public ObservableCollection<Player> Players { get; } = new();

    private Quest _quest = new();
    public Quest Quest
    {
        get => _quest;
        set => SetField(ref _quest, value);
    }

    public OverlaySettings Settings => _settings.Settings;

    public void NotifySettingsChanged() => OnPropertyChanged(nameof(Settings));

    public void HidePart(string partName)
    {
        _hiddenParts.Add(partName);
    }

    public void ShowAllParts()
    {
        _hiddenParts.Clear();
        foreach (var m in Monsters)
            foreach (var p in m.Parts)
                p.IsHidden = false;
    }

    public OverlayViewModel(UpdateLoop updateLoop, SettingsService settings)
    {
        _updateLoop = updateLoop;
        _settings = settings;

        // Live-update overlay when settings change
        _settings.SettingsChanged += () =>
        {
            Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(Settings)));
        };

        _updateLoop.ConnectionChanged += connected =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = connected;
                StatusText = connected ? L.Get("Connected") : L.Get("SearchingMHW");
            });
        };

        _updateLoop.MonstersUpdated += monsters =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Monsters.Clear();
                foreach (var m in monsters)
                {
                    // Restore hidden state from tracked set
                    foreach (var p in m.Parts)
                    {
                        if (_hiddenParts.Contains(p.Name))
                            p.IsHidden = true;
                    }
                    // Track Alatreon element threshold
                    if (m.IsAlatreon)
                    {
                        // Reset max when topple count changes (new cycle) or new quest
                        if (m.AlatreonToppleCount != _alatreonLastTopple)
                        {
                            _alatreonElementMax = 0;
                            _alatreonLastTopple = m.AlatreonToppleCount;
                        }
                        // Also reset if remaining jumps way above max (new quest)
                        if (m.AlatreonElementRemaining > _alatreonElementMax * 1.5f && _alatreonElementMax > 0)
                            _alatreonElementMax = 0;

                        if (m.AlatreonElementRemaining > _alatreonElementMax)
                            _alatreonElementMax = m.AlatreonElementRemaining;

                        m.AlatreonElementMax = _alatreonElementMax;
                    }

                    Monsters.Add(m);
                }
            });
        };

        _updateLoop.PlayersUpdated += players =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Players.Clear();
                foreach (var p in players) Players.Add(p);
            });
        };

        _updateLoop.QuestUpdated += quest =>
        {
            Application.Current.Dispatcher.Invoke(() => Quest = quest);
        };
    }
}
