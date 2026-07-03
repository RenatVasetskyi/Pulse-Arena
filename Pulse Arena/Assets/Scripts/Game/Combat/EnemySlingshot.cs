using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class EnemySlingshot : MonoBehaviour
    {
        private IInputService _inputService;
        private SlingshotData _data;
        private EnemyController _grabbedEnemy;
        private LineRenderer _line;
        private Material _lineMaterial;
        private float _holdAngle;
        private float _cooldownTimer;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.SlingshotData;
        }

        private void Update()
        {
            TickCooldown();

            if (_grabbedEnemy == null && _inputService.IsSlingshotPressedThisFrame)
                TryGrabEnemy();

            if (_grabbedEnemy != null &&
                (_inputService.IsSlingshotReleasedThisFrame || _inputService.IsOrbitBurstPressedThisFrame))
            {
                LaunchGrabbedEnemy();
            }

            UpdateLine();
        }

        private void FixedUpdate()
        {
            if (_grabbedEnemy == null)
                return;

            _holdAngle += _data.HoldAngularSpeed * Time.fixedDeltaTime;
            _grabbedEnemy.MoveGrabbed(GetHoldPosition(), _data.HoldFollowSpeed);
        }

        private void TryGrabEnemy()
        {
            if (_cooldownTimer > 0f)
                return;

            EnemyController enemy = FindNearestEnemy();

            if (enemy == null)
                return;

            _grabbedEnemy = enemy;
            _grabbedEnemy.Grab();
            _holdAngle = Vector3.SignedAngle(Vector3.forward,
                GetPlanarDirectionTo(enemy.transform.position), Vector3.up);
            EnsureLine();
        }

        private EnemyController FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _data.GrabRadius, _data.EnemyLayer);
            EnemyController nearestEnemy = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                if (enemy == null)
                    continue;

                float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;

                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearestEnemy = enemy;
            }

            return nearestEnemy;
        }

        private void LaunchGrabbedEnemy()
        {
            Vector3 launchDirection = GetLaunchDirection();
            Vector3 velocity = (launchDirection + Vector3.up * _data.LaunchUpwardRatio).normalized *
                _data.LaunchForce;

            _grabbedEnemy.Launch(velocity, _data.LaunchDuration);
            _grabbedEnemy = null;
            _cooldownTimer = _data.Cooldown;
            HideLine();
        }

        private Vector3 GetLaunchDirection()
        {
            Vector2 moveDirection = _inputService.MoveDirection;

            if (moveDirection.sqrMagnitude > 0.01f)
                return new Vector3(moveDirection.x, 0f, moveDirection.y).normalized;

            Vector3 direction = transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                direction = Vector3.forward;

            return direction.normalized;
        }

        private Vector3 GetHoldPosition()
        {
            Vector3 direction = Quaternion.Euler(0f, _holdAngle, 0f) * Vector3.forward;
            return transform.position + Vector3.up * _data.HoldHeight + direction * _data.HoldRadius;
        }

        private Vector3 GetPlanarDirectionTo(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return transform.forward;

            return direction.normalized;
        }

        private void EnsureLine()
        {
            if (_line != null)
                return;

            GameObject lineObject = new("Enemy Slingshot Tether");
            lineObject.transform.SetParent(transform, false);
            _line = lineObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.widthMultiplier = _data.LineWidth;
            _line.numCapVertices = 4;
            _line.material = GetLineMaterial();
        }

        private void UpdateLine()
        {
            if (_line == null)
                return;

            if (_grabbedEnemy == null)
            {
                HideLine();
                return;
            }

            _line.enabled = true;
            _line.startColor = _data.LineColor;
            _line.endColor = _data.LineColor;
            _line.SetPosition(0, transform.position + Vector3.up * _data.HoldHeight);
            _line.SetPosition(1, _grabbedEnemy.transform.position);
        }

        private void HideLine()
        {
            if (_line != null)
                _line.enabled = false;
        }

        private Material GetLineMaterial()
        {
            if (_lineMaterial != null)
                return _lineMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            _lineMaterial = new Material(shader)
            {
                name = "Enemy Slingshot Tether"
            };

            return _lineMaterial;
        }

        private void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }
    }
}
