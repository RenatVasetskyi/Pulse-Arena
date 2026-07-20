using System;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     <see cref="ILevelService" /> reading the roster straight from <see cref="GameSettings.Levels" />. Holds only
    ///     the transient selection (which level the player tapped) — persistence of unlock/stars is a separate concern
    ///     (<see cref="ILevelProgressService" />). Plain C#, ProjectContext-scoped.
    /// </summary>
    public class LevelService : ILevelService
    {
        private readonly GameSettings _gameSettings;
        private int _selectedIndex;

        public LevelService(GameSettings gameSettings)
        {
            _gameSettings = gameSettings;
        }

        public IReadOnlyList<LevelDefinition> Levels =>
            _gameSettings.Levels ?? Array.Empty<LevelDefinition>();

        public int Count => Levels.Count;

        public int SelectedIndex => _selectedIndex;

        public LevelDefinition Selected =>
            _selectedIndex >= 0 && _selectedIndex < Levels.Count ? Levels[_selectedIndex] : null;

        public void Select(int index)
        {
            _selectedIndex = Count == 0 ? 0 : Mathf.Clamp(index, 0, Count - 1);
        }
    }
}
