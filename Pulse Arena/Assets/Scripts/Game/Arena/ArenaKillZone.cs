using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Arena
{
    public class ArenaKillZone : MonoBehaviour
    {
        private IScoreService _scoreService;
        private EnemyData _enemyData;

        [Inject]
        public void Construct(IScoreService scoreService, GameSettings gameSettings)
        {
            _scoreService = scoreService;
            _enemyData = gameSettings.EnemyData;
        }

        private void OnTriggerEnter(Collider other)
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();

            if (enemy == null)
                return;

            _scoreService.Add(_enemyData.ScoreReward);
            Destroy(enemy.gameObject);
        }
    }
}
