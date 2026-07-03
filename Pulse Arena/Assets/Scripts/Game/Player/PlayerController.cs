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
        }

        private void Move()
        {
            Vector2 input = _inputService.MoveDirection;
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            _rigidbody.linearVelocity = direction * _data.MoveSpeed;
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
