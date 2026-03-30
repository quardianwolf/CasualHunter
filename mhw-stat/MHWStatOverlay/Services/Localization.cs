using System.Globalization;

namespace MHWStatOverlay.Services;

public static class L
{
    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["tr"] = new()
        {
            // App
            ["AppName"] = "CasualHunter",
            ["Settings"] = "Ayarlar",
            ["Exit"] = "Çıkış",
            ["Save"] = "Kaydet",
            ["Reset"] = "Sıfırla",

            // Status
            ["SearchingMHW"] = "MHW aranıyor...",
            ["Connected"] = "Bağlandı",
            ["Disconnected"] = "Bağlantı kesildi",

            // Tabs
            ["TabGeneral"] = "Genel",
            ["TabMonster"] = "Canavar",
            ["TabDamage"] = "Hasar",
            ["TabQuest"] = "Görev",

            // General settings
            ["GeneralSettings"] = "Genel Ayarlar",
            ["PollingRate"] = "Güncelleme Hızı (ms):",
            ["Opacity"] = "Opaklık:",
            ["OverlayLocked"] = "Overlay kilitli (sürüklenemez)",

            // Monster settings
            ["MonsterHPWidget"] = "Canavar HP Widget",
            ["ShowMonsterWidget"] = "Canavar widget göster",
            ["ShowHPBar"] = "HP bar göster",
            ["ShowHPValues"] = "HP değerleri göster",
            ["Width"] = "Genişlik:",

            // Part settings
            ["PartWidget"] = "Parça Widget",
            ["ShowPartWidget"] = "Parça widget göster",
            ["ShowBreakCount"] = "Kırılma sayısı göster",

            // Damage settings
            ["DamageWidget"] = "Hasar Tablosu Widget",
            ["ShowDamageWidget"] = "Hasar tablosu göster",
            ["ShowDamageNumbers"] = "Hasar sayıları göster",
            ["ShowPercentage"] = "Yüzde göster",

            // Quest settings
            ["QuestWidget"] = "Görev Widget",
            ["ShowQuestWidget"] = "Görev widget göster",

            // Overlay
            ["Damage"] = "HASAR",
            ["Parts"] = "PARÇALAR",
            ["Element"] = "ELEMENT",
            ["EditMode"] = "DÜZENLE",
            ["InsertToClose"] = "Insert ile kapat",
            ["InsertToEdit"] = "Insert ile düzenle",
            ["ShowAllParts"] = "Tüm Parçaları Göster",
            ["ClickPartToHide"] = "Parçaya tıkla gizle",
            ["PressInsert"] = "Overlay düzenlemek için Insert'e bas",
        },
        ["en"] = new()
        {
            ["AppName"] = "CasualHunter",
            ["Settings"] = "Settings",
            ["Exit"] = "Exit",
            ["Save"] = "Save",
            ["Reset"] = "Reset",

            ["SearchingMHW"] = "Searching for MHW...",
            ["Connected"] = "Connected",
            ["Disconnected"] = "Disconnected",

            ["TabGeneral"] = "General",
            ["TabMonster"] = "Monster",
            ["TabDamage"] = "Damage",
            ["TabQuest"] = "Quest",

            ["GeneralSettings"] = "General Settings",
            ["PollingRate"] = "Polling Rate (ms):",
            ["Opacity"] = "Opacity:",
            ["OverlayLocked"] = "Overlay locked (not draggable)",

            ["MonsterHPWidget"] = "Monster HP Widget",
            ["ShowMonsterWidget"] = "Show monster widget",
            ["ShowHPBar"] = "Show HP bar",
            ["ShowHPValues"] = "Show HP values",
            ["Width"] = "Width:",

            ["PartWidget"] = "Part Widget",
            ["ShowPartWidget"] = "Show part widget",
            ["ShowBreakCount"] = "Show break count",

            ["DamageWidget"] = "Damage Board Widget",
            ["ShowDamageWidget"] = "Show damage board",
            ["ShowDamageNumbers"] = "Show damage numbers",
            ["ShowPercentage"] = "Show percentage",

            ["QuestWidget"] = "Quest Widget",
            ["ShowQuestWidget"] = "Show quest widget",

            ["Damage"] = "DAMAGE",
            ["Parts"] = "PARTS",
            ["Element"] = "ELEMENT",
            ["EditMode"] = "EDIT",
            ["InsertToClose"] = "Insert to close",
            ["InsertToEdit"] = "Insert to edit",
            ["ShowAllParts"] = "Show All Parts",
            ["ClickPartToHide"] = "Click part to hide",
            ["PressInsert"] = "Press Insert to edit overlay layout",
        }
    };

    private static string _lang = "en";

    public static void Init()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _lang = Strings.ContainsKey(culture) ? culture : "en";
    }

    public static string Get(string key)
    {
        if (Strings.TryGetValue(_lang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        if (Strings["en"].TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }
}
