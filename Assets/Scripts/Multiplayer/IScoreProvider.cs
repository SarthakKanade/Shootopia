using System.Collections.Generic;

namespace Shootopia
{
    public struct ScoreDisplayEntry
    {
        public string Label;
        public string Value;
    }

    public struct ScoreDisplayData
    {
        public List<ScoreDisplayEntry> Entries;
    }

    public interface IScoreProvider
    {
        ScoreDisplayData GetScoreDisplayData();
    }
}
