
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace FleaSimulator;

public static class FleaSimulatorMetaData
{
    public record ModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.minesettimi.advfleasimulator";
        public string Name { get; init; } = "Advanced Flea Market Simulator";
        public string Author { get; init; } = "minesettimi";
        public List<string>? Contributors { get; init; }
        public Version Version { get; init; } = new(2, 0, 0);
        public Range SptVersion { get; init; } = new("~4.1.0");
        public bool HasPrepatcher { get; init; }

        public List<string>? Incompatibilities { get; init; } = [];
        public Dictionary<string, Range>? ModDependencies { get; init; }

        public string? Url { get; init; } = "https://github.com/minesettimi/FleaSimulator";
        public string License { get; init; } = "MIT";
    }
}