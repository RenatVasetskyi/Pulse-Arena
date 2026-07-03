using System;
using Data;
using Game.Combat;
using Game.Player.Interfaces;
using Game.Pulse;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    public class PlayerFactory : IPlayerFactory
    {
        private readonly DiContainer _container;
        private readonly GameSettings _gameSettings;

        public PlayerFactory(DiContainer container, GameSettings gameSettings)
        {
            _container = container;
            _gameSettings = gameSettings;
        }

        public PlayerController Create(Vector3 at, Quaternion rotation, Transform parent)
        {
            if (_gameSettings.Prefabs.PlayerPrefab == null)
                throw new InvalidOperationException("Player prefab is not assigned in GameSettings.");

            PlayerController player = _container.InstantiatePrefabForComponent<PlayerController>
                (_gameSettings.Prefabs.PlayerPrefab, at, rotation, parent);

            DisableLegacyPulse(player);
            DisableOrbitCutter(player);
            AddCombatComponents(player);

            return player;
        }

        private void DisableLegacyPulse(PlayerController player)
        {
            PulseAbility pulseAbility = player.GetComponentInChildren<PulseAbility>();

            if (pulseAbility != null)
                pulseAbility.enabled = false;
        }

        private void DisableOrbitCutter(PlayerController player)
        {
            OrbitCutter orbitCutter = player.GetComponentInChildren<OrbitCutter>();

            if (orbitCutter != null)
                orbitCutter.enabled = false;
        }

        private void AddCombatComponents(PlayerController player)
        {
            EnemySlingshot enemySlingshot = player.GetComponent<EnemySlingshot>();
            enemySlingshot ??= player.gameObject.AddComponent<EnemySlingshot>();
            _container.Inject(enemySlingshot);
        }
    }
}
