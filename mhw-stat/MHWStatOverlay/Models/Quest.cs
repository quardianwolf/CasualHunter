namespace MHWStatOverlay.Models;

public class Quest
{
    public int Id { get; set; }
    public string Name { get; set; } = "Unknown Quest";
    public QuestType Type { get; set; }
    public float TimerSeconds { get; set; }
    public bool IsActive { get; set; }
    public int MaxFaints { get; set; } = 3;
    public int CurrentFaints { get; set; }
    public int RemainingFaints => MaxFaints - CurrentFaints;

    public string TimerFormatted
    {
        get
        {
            var ts = TimeSpan.FromSeconds(TimerSeconds);
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }
    }
}

public enum QuestType
{
    None,
    Hunt,
    Capture,
    Slay,
    Delivery,
    Event,
    Special,
    Arena
}
