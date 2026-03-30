using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MHWStatOverlay.Models;

public class MonsterPart : INotifyPropertyChanged
{
    public int Index { get; set; }
    public string Name { get; set; } = "Part";
    public float CurrentHP { get; set; }
    public float MaxHP { get; set; }
    public int BreakCount { get; set; }
    public float HPPercentage => MaxHP > 0 ? (CurrentHP / MaxHP) * 100f : 0f;

    private bool _isHidden;
    public bool IsHidden
    {
        get => _isHidden;
        set { _isHidden = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
