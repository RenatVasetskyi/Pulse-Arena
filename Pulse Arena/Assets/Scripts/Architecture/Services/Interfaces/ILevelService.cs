using System.Collections.Generic;
using Data;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     The campaign roster (from <c>LevelConfig.Levels</c>) plus the currently-selected level. The level-select UI
    ///     calls <see cref="Select" />; the game scene's spawners + world builder read <see cref="Selected" /> at match
    ///     build to configure that level's waves + hazard toggles. ProjectContext-scoped so the choice survives the
    ///     scene load into the match.
    /// </summary>
    public interface ILevelService
    {
        IReadOnlyList<LevelDefinition> Levels { get; }

        int Count { get; }

        int SelectedIndex { get; }

        /// <summary>The chosen level, or null when the campaign has no levels authored yet.</summary>
        LevelDefinition Selected { get; }

        void Select(int index);
    }
}
