using System;
using System.Collections.Generic;
using System.Reflection;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

namespace Trapline
{
    public static class TrapLogic
    {
        private struct BirdCatch
        {
            public GearItem Gi;
            public float Hours;
        }

        private static SnareItem[] _all = new SnareItem[0];
        private static Vector3[] _settle = new Vector3[0];
        private static readonly HashSet<string> _warned = new HashSet<string>();
        private static readonly HashSet<int> _seen = new HashSet<int>();
        private static readonly HashSet<int> _roofNoted = new HashSet<int>();
        private static readonly List<BirdCatch> _birds = new List<BirdCatch>();
        private static float _lastDiag = -100f;
        private static int _lastCount = -1;
        private static float _nextTick;
        private static bool _tickLogged;
        private static bool _indoorScene;
        private static bool _indoorLogged;
        private static bool _lastIndoor;

        // Маркер в m_HoursBeforeRoll: в силке лежит наша куропатка. Значение сохраняется в сейве.
        private const float BirdInside = -1f;

        public static void OnSnareUpdate(SnareItem s)
        {
            Cfg.Init();

            float rt = Time.realtimeSinceStartup;
            if (rt < _nextTick) return;
            _nextTick = rt + 2f;

            if (!_tickLogged)
            {
                _tickLogged = true;
                ModLog.Msg("Trapline: тик через SnareItem.Update пошёл");
            }

            TickAll();
        }

        public static void TickAll()
        {
            try
            {
                TickCore();
            }
            catch (Exception e)
            {
                WarnOnce("tick:" + e.GetType().Name + e.Message, "TickAll: " + e);
            }
        }

        private static void TickCore()
        {
            var tod = TimeOfDay.Instance;
            if (tod == null) { WarnOnce("tod", "TimeOfDay.Instance == null"); return; }

            float now = tod.GetHoursPlayedNotPaused();
            var snares = UnityEngine.Object.FindObjectsOfType<SnareItem>();
            if (snares == null || snares.Length == 0) { WarnOnce("none", "силков в сцене нет"); return; }

            _indoorScene = SceneIsIndoor();
            if (!_indoorLogged || _indoorScene != _lastIndoor)
            {
                _indoorLogged = true;
                _lastIndoor = _indoorScene;
                ModLog.Msg(_indoorScene
                    ? "Trapline: закрытая сцена (пещера/помещение) — силки не работают"
                    : "Trapline: открытая сцена");
            }

            BeginPass(snares);

            if (Cfg.ForceNow) { Cfg.ForceNow = false; ForceAll(); }
            if (Cfg.DumpNow) { Cfg.DumpNow = false; DumpAll(); }

            KeepBirdsFresh(now);

            foreach (var sn in snares)
            {
                try { TickSnare(sn, now); }
                catch (Exception e) { WarnOnce("snare:" + e.GetType().Name, e.ToString()); }
            }

            Diag(now, snares);
        }

        public static void ForceAll()
        {
            var tod = TimeOfDay.Instance;
            if (tod == null) { ModLog.Warning("Trapline: ForceNow при TimeOfDay == null"); return; }

            float now = tod.GetHoursPlayedNotPaused();
            var snares = UnityEngine.Object.FindObjectsOfType<SnareItem>();
            ModLog.Msg($"Trapline: принудительный розыгрыш, силков {snares.Length}");

            BeginPass(snares);

            foreach (var s in snares)
            {
                if (s == null) continue;
                if (s.GetState() != SnareState.Set)
                {
                    ModLog.Msg("  пропуск: силок не взведён");
                    continue;
                }
                if (!UsableSpace(s))
                {
                    ModLog.Msg("  пропуск: место не рабочее (закрытая сцена или под крышей)");
                    continue;
                }
                try { Resolve(s, now); }
                catch (Exception e) { ModLog.Warning("Trapline: ForceNow ошибка: " + e); }
            }
        }

        public static void DumpAll()
        {
            var tod = TimeOfDay.Instance;
            if (tod == null) { ModLog.Warning("Trapline: DumpNow при TimeOfDay == null"); return; }

            float now = tod.GetHoursPlayedNotPaused();
            var snares = UnityEngine.Object.FindObjectsOfType<SnareItem>();
            ModLog.Msg($"Trapline: дамп, now={now:F2} ч, indoorscene={_indoorScene}, силков {snares.Length}");

            foreach (var s in snares)
            {
                if (s == null) continue;
                ModLog.Msg(
                    $"  state={s.GetState()} int={s.m_HoursBeforeRoll:F2} last={s.m_HoursAtLastRoll:F2} " +
                    $"roof={HasRoof(s)} pos={s.transform.position} " +
                    $"rabbitRef={(s.m_RabbitAssetReference != null)} spawn={(s.m_RabbitSpawnPoint != null)}");
            }
        }

        public static void BeginPass(SnareItem[] snares)
        {
            _all = snares;

            var pts = new List<Vector3>(32);
            foreach (var f in UnityEngine.Object.FindObjectsOfType<Fire>())
                if (f != null) pts.Add(f.transform.position);
            foreach (var b in UnityEngine.Object.FindObjectsOfType<Bed>())
                if (b != null) pts.Add(b.transform.position);
            _settle = pts.ToArray();
        }

        public static void TickSnare(SnareItem s, float now)
        {
            if (s == null) return;

            var state = s.GetState();

            // Первое появление объекта в сессии: снимаем «наследство» с подобранного силка.
            if (_seen.Add(s.GetInstanceID()))
            {
                if (state == SnareState.Broken)
                {
                    s.SetState(SnareState.Default);
                    state = SnareState.Default;
                }
                else if (state == SnareState.WithRabbit && s.m_HoursBeforeRoll < 0f)
                {
                    // Наша куропатка: тушка лежит отдельным предметом, сам силок пуст.
                    s.SetState(SnareState.Default);
                    state = SnareState.Default;
                }
            }

            // Лежит на земле — не работает и не взводится.
            if (state == SnareState.Default) return;

            // Добыча в силке: ждём, пока её заберут.
            if (state == SnareState.WithRabbit) return;

            // Сработал пустой: до перевзвода реролла нет.
            if (state == SnareState.Broken) return;

            // Закрытая сцена или крыша над силком — не работает.
            if (!UsableSpace(s)) return;

            float wait = Cfg.WaitHours;

            // Интервал не наш — значит силок только что поставлен.
            if (Mathf.Abs(s.m_HoursBeforeRoll - wait) > 2f) { Arm(s, now); return; }

            if (now - s.m_HoursAtLastRoll < wait) return;

            Resolve(s, now);
        }

        private static bool SceneIsIndoor()
        {
            try
            {
                var w = GameManager.GetWeatherComponent();
                return w != null && w.IsIndoorScene();
            }
            catch
            {
                return false;
            }
        }

        private static bool HasRoof(SnareItem s)
        {
            if (s == null) return false;

            RaycastHit hit;
            Vector3 origin = s.transform.position + Vector3.up * 0.5f;
            return Physics.Raycast(origin, Vector3.up, out hit, 12f);
        }

        private static bool UsableSpace(SnareItem s)
        {
            if (!Cfg.CaveCheck) return true;

            if (_indoorScene)
            {
                if (_roofNoted.Add(s.GetInstanceID()))
                    ModLog.Msg("Trapline: закрытая сцена — силок не работает");
                return false;
            }

            if (!HasRoof(s)) return true;

            if (_roofNoted.Add(s.GetInstanceID()))
                ModLog.Msg("Trapline: силок под крышей — не работает");

            return false;
        }

        private static void Arm(SnareItem s, float now)
        {
            float q = Quality(s);
            Schedule(s, now);
            s.SetState(SnareState.Set);

            if (!Cfg.Quiet)
                ModLog.Msg($"Trapline: взведён Q={q:F2}, реролл через {Cfg.WaitHours:F0} ч");
        }

        private static void Schedule(SnareItem s, float now)
        {
            s.m_HoursBeforeRoll = Cfg.WaitHours;
            s.m_HoursAtLastRoll = now;
        }

        private static void Resolve(SnareItem s, float now)
        {
            float q = Quality(s);
            float pCatch = Mathf.Lerp(Cfg.CatchMin, Cfg.CatchMax, q);
            float pEscape = Mathf.Lerp(Cfg.EscapeMax, Cfg.EscapeMin, q);
            float roll = UnityEngine.Random.value;

            Schedule(s, now);

            if (roll < pCatch)
            {
                Catch(s, now, q);
                return;
            }

            if (roll < pCatch + pEscape)
            {
                // Сработал пустой: силок разряжен, пока его не перевзведут.
                s.SetState(SnareState.Broken);
                if (!Cfg.Quiet) ModLog.Msg($"Trapline: побег Q={q:F2}");
                return;
            }

            // Промах: остаётся настороженным до следующего реролла.
            s.SetState(SnareState.Set);
            if (!Cfg.Quiet) ModLog.Msg($"Trapline: промах Q={q:F2}");
        }

        private static void Catch(SnareItem s, float now, float q)
        {
            float species = UnityEngine.Random.value;
            float birdChance = Cfg.PtarmiganChance;

            if (species >= birdChance)
            {
                s.SetState(SnareState.WithRabbit);
                s.SpawnDeadRabbitOnSnare(s.m_RabbitAssetReference);
                if (!Cfg.Quiet)
                    ModLog.Msg($"Trapline: кролик, Q={q:F2}, вид={species:F2} (порог {birdChance:F2})");
                return;
            }

            // Куропатка: меш «добыча в силке», тушка рядом, служебный маркер в интервале.
            s.m_HoursBeforeRoll = BirdInside;
            s.SetState(SnareState.WithRabbit);
            SpawnPtarmigan(s, now);

            if (!Cfg.Quiet)
                ModLog.Msg($"Trapline: куропатка, Q={q:F2}, вид={species:F2} (порог {birdChance:F2})");
        }

        private static void SpawnPtarmigan(SnareItem s, float now)
        {
            Transform anchor = s.m_RabbitSpawnPoint != null ? s.m_RabbitSpawnPoint.transform : s.transform;
            GameObject prefab = LoadPrefab("GEAR_PtarmiganCarcass");

            if (prefab != null)
            {
                var go = UnityEngine.Object.Instantiate(prefab, anchor.position, anchor.rotation) as GameObject;
                if (go != null)
                {
                    var gi = go.GetComponent<GearItem>();
                    if (gi != null)
                    {
                        float before = gi.GetNormalizedCondition();
                        gi.SetNormalizedHP(1f, true);
                        _birds.Add(new BirdCatch { Gi = gi, Hours = now });

                        ModLog.Msg($"Trapline: тушка куропатки создана, состояние {before * 100f:F0}% → 100%");
                    }
                    else
                    {
                        ModLog.Msg("Trapline: тушка куропатки создана без GearItem");
                    }
                }
                return;
            }

            var pm = GameManager.GetPlayerManagerComponent();
            if (pm != null)
            {
                pm.InstantiateItemInPlayerInventory("GEAR_PtarmiganCarcass", 1);
                ModLog.Msg("Trapline: тушка куропатки ушла в рюкзак");
            }
        }

        private static void KeepBirdsFresh(float now)
        {
            if (_birds.Count == 0) return;

            float keep = Cfg.FreshHours;

            for (int i = _birds.Count - 1; i >= 0; i--)
            {
                var b = _birds[i];

                if (now - b.Hours >= keep) { _birds.RemoveAt(i); continue; }

                try
                {
                    if (b.Gi == null) { _birds.RemoveAt(i); continue; }
                    if (b.Gi.GetNormalizedCondition() < 0.999f) b.Gi.SetNormalizedHP(1f, true);
                }
                catch
                {
                    _birds.RemoveAt(i);
                }
            }
        }

        private static bool _loaderScanned;
        private static MethodInfo _gearLoader;
        private static MethodInfo _addrLoader;

        private static GameObject LoadPrefab(string name)
        {
            // 1) Родной загрузчик префабов на GearItem, если он есть.
            if (!_loaderScanned)
            {
                _loaderScanned = true;

                foreach (var m in typeof(GearItem).GetMethods(
                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (m.ReturnType != typeof(GameObject)) continue;
                    var ps = m.GetParameters();
                    if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;
                    if (!m.Name.Contains("Prefab")) continue;

                    _gearLoader = m;
                    ModLog.Msg("Trapline: загрузчик префабов найден: GearItem." + m.Name);
                    break;
                }
            }

            if (_gearLoader != null)
            {
                try
                {
                    var go = _gearLoader.Invoke(null, new object[] { name }) as GameObject;
                    if (go != null) return go;
                }
                catch (Exception e)
                {
                    ModLog.Warning("Trapline: GearItem-загрузчик: " + e.Message);
                }
            }

            // 2) Addressables: тип ищем по короткому имени, приоритет — перегрузка со string.
            if (_addrLoader == null)
            {
                MethodInfo byString = null, any = null;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }

                    foreach (var t in types)
                    {
                        if (t.Name != "Addressables" || t.Namespace == null) continue;
                        if (!t.Namespace.Contains("AddressableAssets")) continue;

                        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (m.Name != "LoadAssetAsync" || !m.IsGenericMethodDefinition) continue;
                            var ps = m.GetParameters();
                            if (ps.Length != 1) continue;

                            if (ps[0].ParameterType == typeof(string)) { byString = m; break; }
                            if (any == null) any = m;
                        }
                        if (byString != null) break;
                    }
                    if (byString != null) break;
                }

                _addrLoader = byString ?? any;
                if (_addrLoader != null)
                    ModLog.Msg("Trapline: LoadAssetAsync найден, параметр " +
                               _addrLoader.GetParameters()[0].ParameterType.FullName);
            }

            if (_addrLoader == null) return null;

            try
            {
                var pt = _addrLoader.GetParameters()[0].ParameterType;
                object arg = pt == typeof(string)
                    ? name
                    : Activator.CreateInstance(pt,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null, new object[] { IL2CPP.ManagedStringToIl2Cpp(name) }, null);

                object handle = _addrLoader.MakeGenericMethod(typeof(GameObject))
                    .Invoke(null, new[] { arg });

                if (handle == null) return null;

                var wait = handle.GetType().GetMethod("WaitForCompletion");
                if (wait != null) return wait.Invoke(handle, null) as GameObject;

                var res = handle.GetType().GetProperty("Result");
                return res != null ? res.GetValue(handle) as GameObject : null;
            }
            catch (Exception e)
            {
                ModLog.Warning("Trapline: Addressables ошибка: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        private static void Diag(float now, SnareItem[] snares)
        {
            float rt = Time.realtimeSinceStartup;
            bool countChanged = snares.Length != _lastCount;
            if (!countChanged && rt - _lastDiag < 30f) return;

            _lastDiag = rt;
            _lastCount = snares.Length;

            var sb = new System.Text.StringBuilder();
            sb.Append($"Trapline: в игре {now / 24f:F1} сут, силков {snares.Length}");

            int shown = 0;
            foreach (var s in snares)
            {
                if (s == null || shown >= 4) continue;
                shown++;

                var state = s.GetState();
                switch (state)
                {
                    case SnareState.Default:
                        sb.Append(" | лежит");
                        break;
                    case SnareState.Broken:
                        sb.Append(" | сработал");
                        break;
                    case SnareState.WithRabbit:
                        sb.Append(s.m_HoursBeforeRoll < 0f ? " | куропатка" : " | кролик");
                        break;
                    default:
                        float left = s.m_HoursBeforeRoll - (now - s.m_HoursAtLastRoll);
                        sb.Append($" | {state} реролл через {left:F0}ч");
                        break;
                }
            }

            if (_birds.Count > 0)
            {
                sb.Append(" | тушки:");
                foreach (var b in _birds)
                    sb.Append(b.Gi == null ? " -" : $" {(b.Gi.GetNormalizedCondition() * 100f):F0}%");
            }

            ModLog.Msg(sb.ToString());
        }

        private static void WarnOnce(string key, string message)
        {
            if (!_warned.Add(key)) return;
            ModLog.Warning("Trapline: " + message);
        }

        private static float Quality(SnareItem s)
        {
            Vector3 p = s.transform.position;
            return Mathf.Clamp01(CrowdFactor(s, p) * SettlementFactor(p));
        }

        private static float CrowdFactor(SnareItem s, Vector3 p)
        {
            int self = s.GetInstanceID();
            float nearest = float.MaxValue;
            foreach (var o in _all)
            {
                if (o == null || o.GetInstanceID() == self) continue;
                float d = Vector3.Distance(p, o.transform.position);
                if (d < nearest) nearest = d;
            }

            if (nearest >= Cfg.SpacingGood) return 1f;
            if (nearest >= Cfg.SpacingBad) return 0.6f;
            if (nearest >= Cfg.SpacingDead) return 0.3f;
            return 0.12f;
        }

        private static float SettlementFactor(Vector3 p)
        {
            float nearest = float.MaxValue;
            foreach (var q in _settle)
            {
                float d = Vector3.Distance(p, q);
                if (d < nearest) nearest = d;
            }

            if (nearest >= Cfg.SettleFar) return 1f;
            if (nearest >= Cfg.SettleNear) return 0.7f;
            return 0.3f;
        }
    }
}