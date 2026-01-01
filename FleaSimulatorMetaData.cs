using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace FleaSimulator;

public static class FleaSimulatorMetaData
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.minesettimi.advfleasimulator";
        public override string Name { get; init; } = "Advanced Flea Market Simulator";
        public override string Author { get; init; } = "minesettimi";
        public override List<string>? Contributors { get; init; }
        public override Version Version { get; init; } = new(0, 6, 1);
        public override Range SptVersion { get; init; } = new("~4.0.4");
        
        public override List<string>? Incompatibilities { get; init; } = ["xyz.drakia.livefleaprices", "com.acidphantasm.delayedfleasales"];
        public override Dictionary<string, Range>? ModDependencies { get; init; }

        public override string? Url { get; init; } = "https://github.com/minesettimi/FleaSimulator";
        public override bool? IsBundleMod { get; init; }
        public override string License { get; init; } = "MIT";
    }
}