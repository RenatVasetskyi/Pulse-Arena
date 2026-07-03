using Architecture.Services.Interfaces;
using Data;
using UnityEngine;
using Zenject;

namespace Game.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;

        private IInputService _inputService;
        private PlayerData _data;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.PlayerData;
        }

        private void Awake()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            RotateToInput();
        }

        private void FixedUpdate()
        {
            Move();
            ApplyExtraGravity();
        }

        private void Move()
        {
            Vector2 input = _inputService.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Vector3 horizontalVelocity = direction * _data.MoveSpeed;

            _rigidbody.linearVelocity = new Vector3(
                horizontalVelocity.x,
                _rigidbody.linearVelocity.y,
                horizontalVelocity.z);
        }

        private void ApplyExtraGravity()
        {
            _rigidbody.AddForce(Vector3.down * _data.ExtraGravity, ForceMode.Acceleration);
        }

        private void RotateToInput()
        {
            Vector2 input = _inputService.MoveDirection;

            if (input.sqrMagnitude <= 0.01f)
                return;

            Vector3 direction = new Vector3(input.x, 0f, input.y);
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                targetRotation, _data.RotationSpeed * Time.deltaTime);
        }
    }
}
