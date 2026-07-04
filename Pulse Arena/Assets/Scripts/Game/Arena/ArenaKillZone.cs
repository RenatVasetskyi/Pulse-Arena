using Game.Enemy;
using Game.Player;
using UnityEngine;

namespace Game.Arena
{
    public class ArenaKillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();

            if (enemy == null)
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                player?.Kill();
                return;
            }

            enemy.Kill();
        }
    }
}
