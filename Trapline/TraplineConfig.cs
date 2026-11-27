using System;
using UnityEngine;

namespace Trapline
{
    public static class Cfg
    {
        internal static TraplineSettings Settings;

        private static bool _registered;
        private static bool _tried;

        public static void Init()
        {
            if (Settings == null)
            {
                Settings = new TraplineSettings();
            }

            if (_registered || _tried) return;
            _tried = true;

            try
            {
                Settings.AddToModSettings("Trapline");

                _registered = true;
                ModLog.Msg("Trapline: настройки зарегистрированы в ModSettings");
            }
            catch (Exception e)
            {
                ModLog.Warning("Trapline: ModSettings не принял настройки: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static float WaitHoursRaw => Settings.WaitHours;
        private static float PtarmiganShareRaw => Settings.PtarmiganShare;
        private static float SpacingMetersRaw => Settings.SpacingMeters;
        private static float SettlementMetersRaw => Settings.SettlementMeters;
        private static float CatchBestRaw => Settings.CatchBest;
        private static float CatchWorstRaw => Settings.CatchWorst;
        private static float EscapeBestRaw => Settings.EscapeBest;
        private static float EscapeWorstRaw => Settings.EscapeWorst;

        public static float WaitHours => Mathf.Max(1f, WaitHoursRaw);
        public static float PtarmiganChance => Mathf.Clamp01(PtarmiganShareRaw / 100f);

        // Тушка куропатки держится свежей 6 игровых часов, дальше портится как обычно.
        public static float FreshHours => 6f;

        public static float SpacingGood => Mathf.Max(1f, SpacingMetersRaw);
        public static float SpacingBad => SpacingGood * 0.48f;
        public static float SpacingDead => SpacingGood * 0.32f;

        public static float SettleFar => Mathf.Max(1f, SettlementMetersRaw);
        public static float SettleNear => SettleFar * 2f / 3f;

        public static float CatchMin => Mathf.Clamp01(Mathf.Min(CatchWorstRaw, CatchBestRaw) / 100f);
        public static float CatchMax => Mathf.Clamp01(Mathf.Max(CatchWorstRaw, CatchBestRaw) / 100f);
        public static float EscapeMin => Mathf.Clamp01(Mathf.Min(EscapeWorstRaw, EscapeBestRaw) / 100f);
        public static float EscapeMax => Mathf.Clamp01(Mathf.Max(EscapeWorstRaw, EscapeBestRaw) / 100f);

        public static bool CaveCheck => Settings.CaveCheck;
        public static bool Quiet => Settings.Quiet;

        public static bool ForceNow
        {
            get => Settings.ForceNow;
            set => Settings.ForceNow = value;
        }

        public static bool DumpNow
        {
            get => Settings.DumpNow;
            set => Settings.DumpNow = value;
        }
    }
}