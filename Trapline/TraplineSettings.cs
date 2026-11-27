using ModSettings;

namespace Trapline
{
    // Один класс настроек: подписи в атрибутах английские, а ModSettingsAttributesPatch
    // подменяет их на лету по ключам из Localization при каждом построении меню.
    public class TraplineSettings : JsonModSettings
    {
        public TraplineSettings() : base("Trapline")
        {
        }

        [Section("Rolls")]

        [Name("Reroll interval, in-game hours")]
        [Description("How many hours a snare waits between attempts. Lower means more food.")]
        [Slider(6f, 72f)]
        public float WaitHours = 24f;

        [Name("Ptarmigan share, %")]
        [Description("Share of ptarmigans in the catch. 0 — rabbits only, 100 — ptarmigans only.")]
        [Slider(0f, 100f)]
        public float PtarmiganShare = 50f;

        [Section("Placement")]

        [Name("Distance between snares, m")]
        [Description("Snares placed closer than this interfere with each other.")]
        [Slider(5f, 50f)]
        public float SpacingMeters = 25f;

        [Name("Distance from shelter, m")]
        [Description("Closer than this a snare works worse, and right next to a campfire it barely works at all.")]
        [Slider(20f, 200f)]
        public float SettlementMeters = 150f;

        [Section("Chances")]

        [Name("Catch chance, perfect placement, %")]
        [Description("Chance to catch something in a well-placed snare.")]
        [Slider(0f, 100f)]
        public float CatchBest = 67f;

        [Name("Catch chance, poor placement, %")]
        [Description("Chance to catch something where the snare works poorly.")]
        [Slider(0f, 100f)]
        public float CatchWorst = 53f;

        [Name("Escape chance, perfect placement, %")]
        [Description("Chance that the animal breaks free from a well-placed snare.")]
        [Slider(0f, 100f)]
        public float EscapeBest = 10f;

        [Name("Escape chance, poor placement, %")]
        [Description("Chance that the animal breaks free from a poorly placed snare.")]
        [Slider(0f, 100f)]
        public float EscapeWorst = 23f;

        [Section("Rules")]

        [Name("Disabled in caves and under roofs")]
        [Description("Snares inside indoor scenes or under a roof never catch anything.")]
        public bool CaveCheck = true;

        [Name("Silent log")]
        [Description("Do not write every arm and roll to the log file.")]
        public bool Quiet = false;

        [Section("Debug")]

        [Name("Roll all snares once")]
        [Description("Force one roll for every armed snare. The flag resets itself.")]
        public bool ForceNow = false;

        [Name("Dump snares to log")]
        [Description("Write the state of every snare to the log once. The flag resets itself.")]
        public bool DumpNow = false;
    }
}