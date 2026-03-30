using System.Collections.ObjectModel;

namespace MHWStatOverlay.Models;

public class Monster
{
    public int Id { get; set; }
    public string MonsterId { get; set; } = "";  // raw ID like "em120_00"
    public string Name { get; set; } = "Unknown";
    public int GameId { get; set; }
    public bool IsAlatreon => GameId == 87 || MonsterId == "em120_00";

    // Alatreon-specific
    public int AlatreonElementState { get; set; }
    public int AlatreonToppleCount { get; set; }
    public float AlatreonElementRemaining { get; set; }  // counts down to 0 = topple
    public float AlatreonElementMax { get; set; }       // tracked max for percentage
    public float AlatreonElementPercentage => AlatreonElementMax > 0
        ? (AlatreonElementRemaining / AlatreonElementMax) * 100f : 0f;
    public bool AlatreonHasThreshold => IsAlatreon && AlatreonElementRemaining > 0;
    public string AlatreonElementName => AlatreonElementState switch
    {
        1 => "FIRE",
        2 => "ICE",
        3 => "DRAGON",
        _ => "---"
    };
    public float CurrentHP { get; set; }
    public float MaxHP { get; set; }
    public float HPPercentage => MaxHP > 0 ? (CurrentHP / MaxHP) * 100f : 0f;
    public float RageTimer { get; set; }
    public float RageDuration { get; set; }
    public bool IsEnraged => RageTimer > 0;
    public float StaminaTimer { get; set; }
    public bool IsExhausted => StaminaTimer > 0;
    public ObservableCollection<MonsterPart> Parts { get; set; } = new();
    public ObservableCollection<MonsterStatusEffect> StatusEffects { get; set; } = new();
}
