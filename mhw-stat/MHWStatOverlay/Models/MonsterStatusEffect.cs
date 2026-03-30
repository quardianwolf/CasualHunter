namespace MHWStatOverlay.Models;

public class MonsterStatusEffect
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public float CurrentBuildup { get; set; }
    public float MaxBuildup { get; set; }
    public float CurrentDuration { get; set; }
    public float MaxDuration { get; set; }
    public int TimesActivated { get; set; }
    public float BuildupPercentage => MaxBuildup > 0 ? (CurrentBuildup / MaxBuildup) * 100f : 0f;
    public bool IsActive => CurrentDuration > 0;
    public bool HasBuildup => MaxBuildup > 0 && CurrentBuildup > 0;

    // Known status effect IDs
    public static readonly Dictionary<int, (string name, string icon)> KnownEffects = new()
    {
        { 0, ("Poison", "\u2620") },      // ☠
        { 1, ("Stun", "\u26A1") },         // ⚡
        { 2, ("Paralysis", "\u2301") },    // ⌁
        { 3, ("Sleep", "\u263D") },         // ☽
        { 4, ("Blast", "\u2738") },         // ✸
        { 5, ("Exhaust", "\u25BC") },       // ▼
        { 6, ("Mount", "\u25B2") },         // ▲
    };
}
