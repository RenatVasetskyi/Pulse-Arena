using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseArena.Tests.PlayMode
{
    /// <summary>
    ///     PlayMode smoke tests for the physics environment the game relies on. Enemies are ragdolled,
    ///     flung and sunk into pits through Rigidbody physics, so these tests pin the baseline assumption
    ///     that a live physics loop actually runs during PlayMode test runs. They intentionally use raw
    ///     Unity objects (no Zenject scene bootstrap) to stay fast and dependency-free.
    /// </summary>
    [TestFixture]
    public class PhysicsTests
    {
        private GameObject _body;

        // Runs after every test, pass or fail — guarantees no test object leaks into the next test.
        [TearDown]
        public void CleanUp()
        {
            if (_body != null)
                Object.Destroy(_body);
        }

        [UnityTest]
        public IEnumerator Rigidbody_FallsUnderGravity()
        {
            _body = new GameObject("PhysicsTestBody");
            _body.AddComponent<Rigidbody>();
            float startY = _body.transform.position.y;

            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(_body.transform.position.y, Is.LessThan(startY));
        }
    }
}
