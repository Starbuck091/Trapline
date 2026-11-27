using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace Trapline
{
    public class TraplineMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Cfg.Init();
            HarmonyInstance.PatchAll();
            ModLog.Msg("Trapline: инициализирован");
        }
    }

    [HarmonyPatch(typeof(SnareItem), nameof(SnareItem.Update))]
    internal static class SnareUpdatePatch
    {
        // Harmony вызывает Prepare во время PatchAll, то есть на старте игры —
        // до того как ModSettings построит интерфейс.
        private static bool Prepare()
        {
            Cfg.Init();
            return true;
        }

        private static void Postfix(SnareItem __instance)
        {
            TrapLogic.OnSnareUpdate(__instance);
        }
    }
}