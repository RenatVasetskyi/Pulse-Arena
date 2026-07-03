using Data;
using System;
using Game.Pulse;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    public class EnergyPickup : MonoBehaviour
    {
        public event Action<EnergyPickup> Collected;

        private PickupData _pickupData;
        private PulseData _pulseData;
        private Vector3 _startPosition;

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _pickupData = gameSettings.PickupData;
            _pulseData = gameSettings.PulseData;
        }

        private void Awake()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, _pickupData.RotateSpeed * Time.deltaTime);

            float yOffset = Mathf.Sin(Time.time * _pickupData.BobSpeed) * _pickupData.BobHeight;
            transform.position = _startPosition + Vector3.up * yOffset;
        }

        private void OnTriggerEnter(Collider other)
        {
            PulseAbility pulseAbility = other.GetComponentInParent<PulseAbility>();

            if (pulseAbility == null)
                return;

            pulseAbility.AddEnergy(_pulseData.EnergyPerPickup);
            Collected?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
