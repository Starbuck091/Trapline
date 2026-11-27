using System;
using System.Collections.Generic;
using System.Globalization;
using Il2Cpp;
using UnityEngine;
using GameLocalization = Il2Cpp.Localization;

namespace Trapline
{
    // Все подписи меню в одном месте: [0] — английский, [1] — русский.
    internal static class Localization
    {
        private static readonly Dictionary<string, string[]> texts = new Dictionary<string, string[]>
        {
            { "SECTION_ROLLS",      new[] { "Rolls", "Розыгрыш" } },
            { "SECTION_PLACEMENT",  new[] { "Placement", "Расстановка" } },
            { "SECTION_CHANCES",    new[] { "Chances", "Шансы" } },
            { "SECTION_RULES",      new[] { "Rules", "Правила" } },
            { "SECTION_DEBUG",      new[] { "Debug", "Отладка" } },

            { "WAIT",               new[] { "Reroll interval, in-game hours", "Интервал реролла, игровых часов" } },
            { "WAIT_DESC",          new[] { "How many hours a snare waits between attempts. Lower means more food.", "Сколько часов силок ждёт между попытками. Меньше — больше добычи." } },

            { "PTARMIGAN",          new[] { "Ptarmigan share, %", "Доля куропаток, %" } },
            { "PTARMIGAN_DESC",     new[] { "Share of ptarmigans in the catch. 0 — rabbits only, 100 — ptarmigans only.", "Доля куропаток в улове. 0 — только кролики, 100 — только куропатки." } },

            { "SPACING",            new[] { "Distance between snares, m", "Дистанция между силками, м" } },
            { "SPACING_DESC",       new[] { "Snares placed closer than this interfere with each other.", "Ближе этого расстояния силки мешают друг другу." } },

            { "SETTLEMENT",         new[] { "Distance from shelter, m", "Дистанция от жилья, м" } },
            { "SETTLEMENT_DESC",    new[] { "Closer than this a snare works worse, and right next to a campfire it barely works at all.", "Ближе этого расстояния силок работает хуже, а вплотную — почти никак." } },

            { "CATCH_BEST",         new[] { "Catch chance, perfect placement, %", "Улов при идеальной установке, %" } },
            { "CATCH_BEST_DESC",    new[] { "Chance to catch something in a well-placed snare.", "Шанс поймать добычу в идеальном месте." } },

            { "CATCH_WORST",        new[] { "Catch chance, poor placement, %", "Улов при плохой установке, %" } },
            { "CATCH_WORST_DESC",   new[] { "Chance to catch something where the snare works poorly.", "Шанс поймать добычу там, где силок работает плохо." } },

            { "ESCAPE_BEST",        new[] { "Escape chance, perfect placement, %", "Побег при идеальной установке, %" } },
            { "ESCAPE_BEST_DESC",   new[] { "Chance that the animal breaks free from a well-placed snare.", "Шанс, что добыча вырвется из правильно поставленного силка." } },

            { "ESCAPE_WORST",       new[] { "Escape chance, poor placement, %", "Побег при плохой установке, %" } },
            { "ESCAPE_WORST_DESC",  new[] { "Chance that the animal breaks free from a poorly placed snare.", "Шанс, что добыча вырвется из плохо поставленного силка." } },

            { "CAVE",               new[] { "Disabled in caves and under roofs", "Не работают в пещерах и под крышей" } },
            { "CAVE_DESC",          new[] { "Snares inside indoor scenes or under a roof never catch anything.", "Силки в закрытых сценах и под крышей не ловят добычу." } },

            { "QUIET",              new[] { "Silent log", "Не писать розыгрыши в лог" } },
            { "QUIET_DESC",         new[] { "Do not write every arm and roll to the log file.", "Отключает подробные записи о каждом взводе и розыгрыше." } },

            { "FORCE",              new[] { "Roll all snares once", "Разовый розыгрыш всех силков" } },
            { "FORCE_DESC",         new[] { "Force one roll for every armed snare. The flag resets itself.", "Один раз разыграть добычу на всех силках. Флаг сбрасывается сам." } },

            { "DUMP",               new[] { "Dump snares to log", "Дамп силков в лог" } },
            { "DUMP_DESC",          new[] { "Write the state of every snare to the log once. The flag resets itself.", "Один раз записать состояние всех силков в лог. Флаг сбрасывается сам." } },
        };

        private static Dictionary<string, string> _byText;
        private static string _lastReport;
        private static Transform _panelRoot;

        internal static bool IsRussian()
        {
            string language = DetectLanguage(out string source);
            bool russian = LooksRussian(language);

            Report(language, source, russian);
            return russian;
        }

        // Главный источник — язык самой игры (статическое поле s_Language).
        // PlayerPrefs и язык системы — только запасные варианты.
        private static string DetectLanguage(out string source)
        {
            string game = SafeGameLanguage();

            if (!string.IsNullOrEmpty(game))
            {
                source = "язык игры s_Language";
                return game;
            }

            string prefs = SafePrefsLanguage();

            if (!string.IsNullOrEmpty(prefs))
            {
                source = "PlayerPrefs Language";
                return prefs;
            }

            source = "язык системы";
            return SafeSystemLanguage();
        }

        internal static string SafeGameLanguage()
        {
            try
            {
                return GameLocalization.s_Language ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string SafePrefsLanguage()
        {
            try
            {
                return PlayerPrefs.GetString("Language", string.Empty) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string SafeSystemLanguage()
        {
            try
            {
                return Application.systemLanguage.ToString();
            }
            catch
            {
            }

            try
            {
                return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string Describe()
        {
            return $"s_Language=«{SafeGameLanguage()}», PlayerPrefs=«{SafePrefsLanguage()}», система=«{SafeSystemLanguage()}»";
        }

        private static bool LooksRussian(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return false;
            }

            // Покрывает ru, RU, ru-RU, Russian, Русский.
            return language.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
                || language.IndexOf("russian", StringComparison.OrdinalIgnoreCase) >= 0
                || language.IndexOf("русск", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void Report(string language, string source, bool russian)
        {
            string key = (language ?? string.Empty) + "|" + source + "|" + russian;

            if (key == _lastReport)
            {
                return;
            }

            _lastReport = key;
            ModLog.Msg($"Trapline: меню настроек — {(russian ? "ru" : "en")}, источник: {source}, {Describe()}");
        }

        internal static string GetRaw(string key)
        {
            if (!texts.TryGetValue(key, out string[] pair))
            {
                return key;
            }

            return IsRussian() ? pair[1] : pair[0];
        }

        // Панель настроек, чтобы потом переписать её подписи без пересборки.
        internal static void TrackPanel(Panel_OptionsMenu panel)
        {
            try
            {
                if (panel != null)
                {
                    _panelRoot = panel.transform;
                }
            }
            catch
            {
            }
        }

        internal static int ApplyTracked()
        {
            return ApplyTrackedReport(out _);
        }

        internal static int ApplyTrackedReport(out string report)
        {
            return Translate(_panelRoot, out report);
        }

        internal static int ApplyToUi(Transform root)
        {
            if (root == null)
            {
                return 0;
            }

            return Translate(root, out _);
        }

        internal static string ApplyToUiReport(Transform root)
        {
            if (root == null)
            {
                return "панель не найдена";
            }

            Translate(root, out string report);
            return report;
        }

        private static int Translate(Transform root, out string report)
        {
            int panelVisited = 0;
            int panelMatched = 0;
            int panelChanged = 0;

            if (root != null)
            {
                TranslateTree(root, ref panelVisited, ref panelMatched, ref panelChanged);
            }

            int sceneVisited = 0;
            int sceneMatched = 0;
            int sceneChanged = 0;

            TranslateScene(ref sceneVisited, ref sceneMatched, ref sceneChanged);

            int matched = panelMatched + sceneMatched;
            int changed = panelChanged + sceneChanged;

            report = $"панель: {panelMatched}/{panelVisited}, сцена: {sceneMatched}/{sceneVisited}, изменено: {changed}";

            if (matched == 0)
            {
                report += " | образцы: " + SampleSceneTexts();
            }

            return changed;
        }

        // Перевод одиночной строки для подписей, которые ModSettings выставляет позже
        // (описания, значения списков).
        internal static string TranslateText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (!ByText().TryGetValue(text.Trim(), out string key))
            {
                return null;
            }

            string value = GetRaw(key);

            return string.Equals(value, text, StringComparison.Ordinal) ? null : value;
        }

        private static void TranslateScene(ref int visited, ref int matched, ref int changed)
        {
            try
            {
                var labels = UnityEngine.Object.FindObjectsOfType<UILabel>(true);

                for (int i = 0; i < labels.Length; i++)
                {
                    UILabel label = labels[i] as UILabel;

                    if (label != null)
                    {
                        TranslateLabel(label, ref visited, ref matched, ref changed);
                    }
                }
            }
            catch
            {
            }
        }

        private static void TranslateLabel(UILabel label, ref int visited, ref int matched, ref int changed)
        {
            visited++;

            string text = label.text;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!ByText().TryGetValue(text.Trim(), out string key))
            {
                return;
            }

            matched++;
            string value = GetRaw(key);

            if (!string.Equals(value, text, StringComparison.Ordinal))
            {
                label.text = value;
                changed++;
            }
        }

        private static string SampleSceneTexts()
        {
            var list = new List<string>();

            try
            {
                var labels = UnityEngine.Object.FindObjectsOfType<UILabel>(true);

                for (int i = 0; i < labels.Length && list.Count < 12; i++)
                {
                    UILabel label = labels[i] as UILabel;

                    if (label == null)
                    {
                        continue;
                    }

                    string text = (label.text ?? string.Empty).Trim();

                    if (text.Length == 0)
                    {
                        continue;
                    }

                    if (text.Length > 40)
                    {
                        text = text.Substring(0, 40) + "…";
                    }

                    list.Add(text);
                }
            }
            catch
            {
            }

            return list.Count == 0 ? "-" : string.Join(" | ", list);
        }

        private static void TranslateTree(Transform node, ref int visited, ref int matched, ref int changed)
        {
            try
            {
                UILabel label = node.GetComponent<UILabel>();

                if (label != null)
                {
                    visited++;
                    string text = label.text;

                    if (!string.IsNullOrEmpty(text) && ByText().TryGetValue(text.Trim(), out string key))
                    {
                        matched++;
                        string value = GetRaw(key);

                        if (!string.Equals(value, text, StringComparison.Ordinal))
                        {
                            label.text = value;
                            changed++;
                        }
                    }
                }

                int count = node.childCount;

                for (int i = 0; i < count; i++)
                {
                    TranslateTree(node.GetChild(i), ref visited, ref matched, ref changed);
                }
            }
            catch
            {
            }
        }

        private static Dictionary<string, string> ByText()
        {
            if (_byText != null)
            {
                return _byText;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string[]> pair in texts)
            {
                map[pair.Value[0].Trim()] = pair.Key;
                map[pair.Value[1].Trim()] = pair.Key;
            }

            _byText = map;
            return _byText;
        }
    }
}