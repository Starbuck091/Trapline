using HarmonyLib;
using System;
using Il2Cpp;
using ModSettings;
using System.Reflection;

namespace Trapline
{
    // ModSettings читает атрибуты в GUIBuilder.AddSettings при каждом построении меню.
    // Здесь мы подменяем английские подписи на нужный язык в момент чтения.
    [HarmonyPatch]
    internal static class ModSettingsAttributesPatch
    {
        private static MethodBase TargetMethod()
        {
            var attributesType = AccessTools.TypeByName("ModSettings.Attributes");
            if (attributesType == null)
            {
                return null;
            }

            return attributesType.GetMethod("GetAttributes", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static void Postfix(
            FieldInfo field,
            ref SectionAttribute section,
            ref NameAttribute name,
            ref DescriptionAttribute description)
        {
            if (field.DeclaringType != typeof(TraplineSettings))
            {
                return;
            }

            try
            {
                if (field.Name == nameof(TraplineSettings.WaitHours))
                {
                    ModLog.Msg($"Trapline: страница настроек строится, меню {(Localization.IsRussian() ? "ru" : "en")}, {Localization.Describe()}");
                }

                if (section != null)
                {
                    string sectionKey = GetSectionKey(section.Title);
                    if (sectionKey != null)
                    {
                        section = new SectionAttribute(Localization.GetRaw(sectionKey));
                    }
                }

                if (name != null)
                {
                    string nameKey = GetNameKey(field.Name);
                    if (nameKey != null)
                    {
                        name = new NameAttribute(Localization.GetRaw(nameKey));
                    }
                }

                if (description != null)
                {
                    string descriptionKey = GetDescriptionKey(field.Name);
                    if (descriptionKey != null)
                    {
                        description = new DescriptionAttribute(Localization.GetRaw(descriptionKey));
                    }
                }
            }
            catch
            {
                // Любая ошибка локализации не должна ломать меню: остаются английские подписи.
            }
        }

        private static string GetSectionKey(string title)
        {
            switch (title)
            {
                case "Rolls":
                    return "SECTION_ROLLS";
                case "Placement":
                    return "SECTION_PLACEMENT";
                case "Chances":
                    return "SECTION_CHANCES";
                case "Rules":
                    return "SECTION_RULES";
                case "Debug":
                    return "SECTION_DEBUG";
                default:
                    return null;
            }
        }

        private static string GetNameKey(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(TraplineSettings.WaitHours):
                    return "WAIT";
                case nameof(TraplineSettings.PtarmiganShare):
                    return "PTARMIGAN";
                case nameof(TraplineSettings.SpacingMeters):
                    return "SPACING";
                case nameof(TraplineSettings.SettlementMeters):
                    return "SETTLEMENT";
                case nameof(TraplineSettings.CatchBest):
                    return "CATCH_BEST";
                case nameof(TraplineSettings.CatchWorst):
                    return "CATCH_WORST";
                case nameof(TraplineSettings.EscapeBest):
                    return "ESCAPE_BEST";
                case nameof(TraplineSettings.EscapeWorst):
                    return "ESCAPE_WORST";
                case nameof(TraplineSettings.CaveCheck):
                    return "CAVE";
                case nameof(TraplineSettings.Quiet):
                    return "QUIET";
                case nameof(TraplineSettings.ForceNow):
                    return "FORCE";
                case nameof(TraplineSettings.DumpNow):
                    return "DUMP";
                default:
                    return null;
            }
        }

        private static string GetDescriptionKey(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(TraplineSettings.WaitHours):
                    return "WAIT_DESC";
                case nameof(TraplineSettings.PtarmiganShare):
                    return "PTARMIGAN_DESC";
                case nameof(TraplineSettings.SpacingMeters):
                    return "SPACING_DESC";
                case nameof(TraplineSettings.SettlementMeters):
                    return "SETTLEMENT_DESC";
                case nameof(TraplineSettings.CatchBest):
                    return "CATCH_BEST_DESC";
                case nameof(TraplineSettings.CatchWorst):
                    return "CATCH_WORST_DESC";
                case nameof(TraplineSettings.EscapeBest):
                    return "ESCAPE_BEST_DESC";
                case nameof(TraplineSettings.EscapeWorst):
                    return "ESCAPE_WORST_DESC";
                case nameof(TraplineSettings.CaveCheck):
                    return "CAVE_DESC";
                case nameof(TraplineSettings.Quiet):
                    return "QUIET_DESC";
                case nameof(TraplineSettings.ForceNow):
                    return "FORCE_DESC";
                case nameof(TraplineSettings.DumpNow):
                    return "DUMP_DESC";
                default:
                    return null;
            }
        }
    }

    // Запоминаем панель настроек и переписываем подписи при смене языка.
    [HarmonyPatch(typeof(Panel_OptionsMenu))]
    internal static class LanguageChangePatch
    {
        private static bool _prepared;

        private static bool Prepare()
        {
            if (!_prepared)
            {
                _prepared = true;
                ModLog.Msg("Trapline: хуки панели настроек применены");
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("InitializeAutosaveMenuItems")]
        private static void Postfix_InitializeAutosaveMenuItems(Panel_OptionsMenu __instance)
        {
            Localization.TrackPanel(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnConfirmLanguage")]
        private static void Postfix_OnConfirmLanguage(Panel_OptionsMenu __instance)
        {
            Localization.TrackPanel(__instance);

            string report = Localization.ApplyToUiReport(__instance.transform);

            ModLog.Msg($"Trapline: OnConfirmLanguage, {report}, {Localization.Describe()}");
        }

        [HarmonyPostfix]
        [HarmonyPatch("RefreshLanguage")]
        private static void Postfix_RefreshLanguage(Panel_OptionsMenu __instance)
        {
            Localization.TrackPanel(__instance);
            Localization.ApplyToUi(__instance.transform);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetLanguage")]
        private static void Postfix_SetLanguage(Panel_OptionsMenu __instance)
        {
            Localization.TrackPanel(__instance);

            string report = Localization.ApplyToUiReport(__instance.transform);

            ModLog.Msg($"Trapline: SetLanguage, {report}, {Localization.Describe()}");
        }
    }

    // Само событие смены языка игры — самый надёжный момент для перевода уже созданных подписей.
    [HarmonyPatch(typeof(Il2Cpp.Localization), "set_Language")]
    internal static class GameLanguageChangedPatch
    {
        private static void Postfix()
        {
            Localization.ApplyTrackedReport(out string report);

            ModLog.Msg($"Trapline: игра сменила язык (set_Language), {report}, {Localization.Describe()}");
        }
    }

    // Некоторые пути смены языка идут через SelectLanguage, а не через сеттер свойства.
    [HarmonyPatch(typeof(Il2Cpp.Localization), "SelectLanguage")]
    internal static class GameSelectLanguagePatch
    {
        private static void Postfix()
        {
            Localization.ApplyTrackedReport(out string report);

            ModLog.Msg($"Trapline: игра сменила язык (SelectLanguage), {report}, {Localization.Describe()}");
        }
    }

    // Подписи, которые ModSettings выставляет уже после сборки страницы
    // (описания, значения списков), переводим в момент присваивания текста.
    [HarmonyPatch(typeof(UILabel), "set_text")]
    internal static class LabelTextPatch
    {
        [ThreadStatic]
        private static bool busy;

        private static void Postfix(UILabel __instance)
        {
            if (busy)
            {
                return;
            }

            string text = __instance.text;
            string translated = Localization.TranslateText(text);

            if (translated == null)
            {
                return;
            }

            try
            {
                busy = true;
                __instance.text = translated;
            }
            catch
            {
            }
            finally
            {
                busy = false;
            }
        }
    }
}