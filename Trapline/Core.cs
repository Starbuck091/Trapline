using MelonLoader;

[assembly: MelonInfo(typeof(Trapline.Core), "Trapline", "1.0.0", "Starbuck097", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace Trapline
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }
    }
}