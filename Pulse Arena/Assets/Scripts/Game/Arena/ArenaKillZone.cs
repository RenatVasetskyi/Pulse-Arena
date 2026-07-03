using Game.Enemy;
using UnityEngine;

namespace Game.Arena
{
    public class ArenaKillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();

            if (enemy == null)
                return;

            enemy.Kill();
        }
    }
}
