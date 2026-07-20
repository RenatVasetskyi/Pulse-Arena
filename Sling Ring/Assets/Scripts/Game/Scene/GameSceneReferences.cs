using Game.Cameras;
using UnityEngine;

namespace Game.Scene
{
    public class GameSceneReferences : MonoBehaviour
    {
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform _playerParent;
        [SerializeField] private Transform _enemySpawnParent;
        [SerializeField] private Transform _pickupSpawnParent;
        [SerializeField] private float _playerSpawnHeightOffset = 1f;
        [SerializeField] private float _enemySpawnHeightOffset = 1f;
        [SerializeField] private float _pickupSpawnHeightOffset = 0.5f;

        public Transform PlayerSpawnPoint => _playerSpawnPoint;
        public Transform PlayerParent => _playerParent;
        public Transform EnemySpawnParent => _enemySpawnParent;
        public Transform PickupSpawnParent => _pickupSpawnParent;
        public float EnemySpawnHeightOffset => ResolveOffset(_enemySpawnHeightOffset, 1f);
        public float PickupSpawnHeightOffset => ResolveOffset(_pickupSpawnHeightOffset, 0.5f);

        public Vector3 PlayerSpawnPosition => AddHeightOffset(_playerSpawnPoint.position,
            ResolveOffset(_playerSpawnHeightOffset, 1f));

        public void Validate()
        {
            if (_battleCamera == null)
                throw new MissingReferenceException($"{nameof(GameSceneReferences)} requires battle camera.");

            if (_playerSpawnPoint == null)
                throw new MissingReferenceException($"{nameof(GameSceneReferences)} requires player spawn point.");
        }

        private static Vector3 AddHeightOffset(Vector3 position, float offset)
        {
            return position + Vector3.up * offset;
        }

        private static float ResolveOffset(float value, float fallback)
        {
            return Mathf.Approximately(value, 0f) ? fallback : value;
        }
    }
}