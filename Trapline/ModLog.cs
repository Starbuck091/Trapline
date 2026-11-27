using MelonLoader;
using MelonLoader.Utils;
using System;
using System.IO;

namespace Trapline
{
    // Сообщения мода пишутся в UserData/Trapline.log. Консоль MelonLoader остаётся чистой,
    // кроме предупреждений об ошибках.
    public static class ModLog
    {
        private static readonly object Gate = new object();
        private static StreamWriter _writer;
        private static bool _tried;

        public static void Msg(string text)
        {
            Append(text);
        }

        public static void Warning(string text)
        {
            MelonLogger.Warning(text);
            Append(text);
        }

        private static void Append(string text)
        {
            lock (Gate)
            {
                if (!_tried) Open();
                if (_writer == null) return;

                try { _writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}"); }
                catch { _writer = null; }
            }
        }

        private static void Open()
        {
            _tried = true;

            try
            {
                string dir = MelonEnvironment.UserDataDirectory;
                if (string.IsNullOrEmpty(dir))
                    dir = Path.Combine(MelonEnvironment.GameRootDirectory, "UserData");

                Directory.CreateDirectory(dir);

                _writer = new StreamWriter(Path.Combine(dir, "Trapline.log"), false) { AutoFlush = true };
                _writer.WriteLine($"=== Trapline, {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            }
            catch (Exception e)
            {
                _writer = null;
                MelonLogger.Warning("Trapline: лог-файл недоступен: " + e.Message);
            }
        }
    }
}