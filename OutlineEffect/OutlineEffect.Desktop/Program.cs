using osu.Framework.Platform;
using osu.Framework;
using OutlineEffect.Game;

namespace OutlineEffect.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"OutlineEffect"))
            using (osu.Framework.Game game = new OutlineEffectGame())
                host.Run(game);
        }
    }
}