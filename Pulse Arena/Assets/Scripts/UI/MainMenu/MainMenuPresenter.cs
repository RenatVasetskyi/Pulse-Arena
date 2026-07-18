using System;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Architecture.States;
using Architecture.States.Interfaces;
using Data;
using UnityEngine;

namespace UI.MainMenu
{
    /// <summary>
    ///     Drives the main menu: Play opens the level-select overlay (built from the campaign roster + saved
    ///     progress), tapping an unlocked level selects it and enters <see cref="LoadGameState" />, Back returns to
    ///     the menu. Settings opens the settings controller. Owns the level-select view's lifecycle.
    /// </summary>
    public class MainMenuPresenter : IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly GameSettings _gameSettings;
        private readonly ILevelProgressService _levelProgress;
        private readonly ILevelService _levelService;
        private readonly ISettingsController _settingsController;
        private readonly IStateMachine _stateMachine;
        private readonly MainMenuView _view;
        private readonly IWindowFactory _windowFactory;
        private LevelSelectView _levelSelect;

        public MainMenuPresenter(MainMenuView view, IStateMachine stateMachine, IAudioService audioService,
            ISettingsController settingsController, ILevelService levelService, ILevelProgressService levelProgress,
            GameSettings gameSettings, IWindowFactory windowFactory)
        {
            _view = view;
            _stateMachine = stateMachine;
            _audioService = audioService;
            _settingsController = settingsController;
            _levelService = levelService;
            _levelProgress = levelProgress;
            _gameSettings = gameSettings;
            _windowFactory = windowFactory;
        }

        public void Initialize()
        {
            _view.PlayClicked += OnPlayClicked;
            _view.SettingsClicked += OnSettingsClicked;
            _view.ResetClicked += OnResetClicked;

            if (_view.ConfirmDialog != null)
            {
                _view.ConfirmDialog.Confirmed += OnResetConfirmed;
                _view.ConfirmDialog.Cancelled += OnResetCancelled;
            }

            _view.Show();
        }

        public void Dispose()
        {
            DestroyLevelSelect();

            if (_view == null)
                return;

            _view.PlayClicked -= OnPlayClicked;
            _view.SettingsClicked -= OnSettingsClicked;
            _view.ResetClicked -= OnResetClicked;

            if (_view.ConfirmDialog != null)
            {
                _view.ConfirmDialog.Confirmed -= OnResetConfirmed;
                _view.ConfirmDialog.Cancelled -= OnResetCancelled;
            }

            _view.Dispose();
        }

        private void OnPlayClicked()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _view.Hide();
            OpenLevelSelect();
        }

        private void OpenLevelSelect()
        {
            if (_levelSelect == null)
                CreateLevelSelect();

            if (_levelSelect == null)
                return;

            _levelSelect.Build(BuildLevelModels());
            BindSurvival();
            _levelSelect.Show();
        }

        private void BindSurvival()
        {
            int index = FindSurvivalIndex();

            if (index < 0)
                return;

            _levelSelect.BindSurvival(index, _levelProgress.GetSurvivalBest());
        }

        private int FindSurvivalIndex()
        {
            IReadOnlyList<LevelDefinition> levels = _levelService.Levels;

            for (int i = 0; i < levels.Count; i++)
                if (levels[i].IsEndless)
                    return i;

            return -1;
        }

        /// <summary>
        ///     Instantiates the level-select overlay once and keeps it. It is shown/hidden on open/close (never
        ///     re-instantiated per open); the instance is destroyed only in <see cref="Dispose" /> on menu teardown.
        /// </summary>
        private void CreateLevelSelect()
        {
            _levelSelect = _windowFactory.Create<LevelSelectView>(_gameSettings.Prefabs.LevelSelectPrefab,
                "LevelSelectPrefab");

            if (_levelSelect == null)
                return;

            _levelSelect.LevelChosen += OnLevelChosen;
            _levelSelect.BackPressed += OnLevelSelectBack;
        }

        private List<LevelSelectView.LevelButtonModel> BuildLevelModels()
        {
            List<LevelSelectView.LevelButtonModel> models = new();
            IReadOnlyList<LevelDefinition> levels = _levelService.Levels;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];

                models.Add(new LevelSelectView.LevelButtonModel
                {
                    Name = level.DisplayName,
                    Unlocked = level.IsEndless || _levelProgress.IsUnlocked(i),
                    Stars = _levelProgress.GetStars(i),
                    IsSurvival = level.IsEndless
                });
            }

            return models;
        }

        private void OnLevelChosen(int index)
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _levelService.Select(index);
            _levelSelect.Hide();
            _stateMachine.Enter<LoadGameState>();
        }

        private void OnLevelSelectBack()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _levelSelect.Hide();
            _view.Show();
        }

        private void DestroyLevelSelect()
        {
            if (_levelSelect == null)
                return;

            _levelSelect.LevelChosen -= OnLevelChosen;
            _levelSelect.BackPressed -= OnLevelSelectBack;
            _levelSelect.Dispose();
            _levelSelect = null;
        }

        private void OnSettingsClicked()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _settingsController?.Open();
        }

        private void OnResetClicked()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _view.ConfirmDialog?.Show();
        }

        private void OnResetConfirmed()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _levelProgress.ResetProgress();
            _view.ConfirmDialog?.Hide();
        }

        private void OnResetCancelled()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _view.ConfirmDialog?.Hide();
        }
    }
}
