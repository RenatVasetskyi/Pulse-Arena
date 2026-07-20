using Data;
using Game.Combat.Interfaces;
using Game.Enemy;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     Drives the on-ground hook-target marker. While the lasso is idle it highlights the nearest enemy found by
    ///     the <see cref="IEnemyTargetFinder" /> — green when grabbable, grey when out of range or on cooldown — and
    ///     hides it otherwise. Lazily instantiates the marker prefab and drives the <see cref="HookTargetMarker" /> view.
    /// </summary>
    public class HookTargetMarkerPresenter : IHookTargetMarkerPresenter
    {
        private SlingshotData _data;
        private IEnemyTargetFinder _finder;
        private HookTargetMarker _marker;
        private Transform _parent;
        private GameObject _prefab;

        public void Initialize(Transform parent, SlingshotData data, GameObject prefab, IEnemyTargetFinder finder)
        {
            _parent = parent;
            _data = data;
            _prefab = prefab;
            _finder = finder;
        }

        public void Tick(bool lassoIdle, bool onCooldown)
        {
            if (!lassoIdle)
            {
                _marker?.Hide();
                return;
            }

            float searchRadius = _data.GrabRadius * Mathf.Max(1f, _data.MarkerSearchRangeMultiplier);
            EnemyController enemy = _finder.FindNearest(searchRadius);

            if (enemy == null)
            {
                _marker?.Hide();
                return;
            }

            if (_marker == null && _prefab != null)
                _marker = Object.Instantiate(_prefab, _parent, false).GetComponent<HookTargetMarker>();

            if (_marker == null)
                return;

            float sqrDistance = (enemy.transform.position - _parent.position).sqrMagnitude;
            bool isGrabbable = !onCooldown && sqrDistance <= _data.GrabRadius * _data.GrabRadius;
            Color color = isGrabbable ? _data.MarkerActiveColor : _data.MarkerInactiveColor;
            float radius = _data.MarkerRadius * Mathf.Max(0.5f, enemy.TypeData.VisualScale);
            Vector3 center = enemy.transform.position + Vector3.up * _data.MarkerHeight;

            _marker.Show(center, radius, _data.MarkerWidth, color,
                _data.MarkerPulseSpeed, _data.MarkerPulseAmplitude);
        }
    }
}