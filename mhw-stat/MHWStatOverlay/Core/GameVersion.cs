namespace MHWStatOverlay.Core;

public enum MHWVersion
{
    Unknown,
    BaseGame,
    Iceborne
}

public class GameVersion
{
    // Iceborne executable is significantly larger than base game
    private const long IceborneMinModuleSize = 60_000_000;

    public static MHWVersion Detect(GameProcess gameProcess)
    {
        long moduleSize = gameProcess.GetModuleSize();
        if (moduleSize == 0)
            return MHWVersion.Unknown;

        return moduleSize >= IceborneMinModuleSize
            ? MHWVersion.Iceborne
            : MHWVersion.BaseGame;
    }
}
