using System.Collections.Generic;
using Game.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace PulseArena.Tests.EditMode.Enemy
{
    /// <summary>
    ///     EditMode unit tests for <see cref="EnemyRegistry" /> — the scene-scoped Rigidbody-to-EnemyController
    ///     reverse-lookup map behind <see cref="Game.Enemy.Interfaces.IEnemyRegistry" />. Tests use REAL
    ///     <see cref="Rigidbody" /> and <see cref="EnemyController" /> components on throwaway GameObjects
    ///     (Awake does not run in EditMode, so adding EnemyController is side-effect free); every created object
    ///     is tracked and DestroyImmediate-d in TearDown. Registration is plain dictionary bookkeeping — no
    ///     physics simulation is stepped anywhere.
    /// </summary>
    [TestFixture]
    public class EnemyRegistryTests
    {
        private EnemyRegistry _registry;
        private List<GameObject> _spawned;

        [SetUp]
        public void SetUp()
        {
            _registry = new EnemyRegistry();
            _spawned = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawnedObject in _spawned)
            {
                if (spawnedObject != null)
                    Object.DestroyImmediate(spawnedObject);
            }
        }

        [Test]
        public void TryResolve_RegisteredBody_ReturnsTrueAndThatEnemy()
        {
            Rigidbody body = CreateBody();
            EnemyController enemy = CreateEnemy();
            _registry.Register(body, enemy);

            bool resolved = _registry.TryResolve(body, out EnemyController result);

            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(enemy));
        }

        [Test]
        public void TryResolve_UnknownBody_ReturnsFalseAndNull()
        {
            Rigidbody body = CreateBody();

            bool resolved = _registry.TryResolve(body, out EnemyController result);

            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryResolve_NullBody_ReturnsFalseAndNull()
        {
            bool resolved = _registry.TryResolve((Rigidbody)null, out EnemyController result);

            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryResolve_ColliderOnRegisteredBody_ReturnsTrueAndThatEnemy()
        {
            // attachedRigidbody resolves without stepping physics — the physics representation exists
            // as soon as the components are added, even in EditMode.
            Rigidbody body = CreateBody();
            BoxCollider collider = body.gameObject.AddComponent<BoxCollider>();
            EnemyController enemy = CreateEnemy();
            _registry.Register(body, enemy);

            bool resolved = _registry.TryResolve(collider, out EnemyController result);

            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(enemy));
        }

        [Test]
        public void TryResolve_ColliderWithoutRigidbody_ReturnsFalseAndNull()
        {
            GameObject host = new GameObject("LooseCollider");
            _spawned.Add(host);
            BoxCollider collider = host.AddComponent<BoxCollider>();

            bool resolved = _registry.TryResolve(collider, out EnemyController result);

            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryResolve_NullCollider_ReturnsFalseAndNull()
        {
            bool resolved = _registry.TryResolve((Collider)null, out EnemyController result);

            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Register_SameBodyTwice_LastRegistrationWins()
        {
            Rigidbody body = CreateBody();
            EnemyController first = CreateEnemy();
            EnemyController second = CreateEnemy();

            _registry.Register(body, first);
            _registry.Register(body, second);

            bool resolved = _registry.TryResolve(body, out EnemyController result);
            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(second));
        }

        [Test]
        public void Register_NullBody_DoesNotThrow()
        {
            EnemyController enemy = CreateEnemy();

            Assert.That(() => _registry.Register(null, enemy), Throws.Nothing);
        }

        [Test]
        public void Register_NullEnemy_DoesNotStoreEntry()
        {
            // The null-enemy guard keeps the "a resolved controller is always a valid target" contract:
            // a hit in the map must never yield null.
            Rigidbody body = CreateBody();

            _registry.Register(body, null);

            bool resolved = _registry.TryResolve(body, out EnemyController result);
            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Unregister_RegisteredBody_RemovesEntry()
        {
            Rigidbody body = CreateBody();
            EnemyController enemy = CreateEnemy();
            _registry.Register(body, enemy);

            _registry.Unregister(body);

            bool resolved = _registry.TryResolve(body, out EnemyController result);
            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Unregister_OneBody_LeavesOtherRegistrationsIntact()
        {
            Rigidbody removed = CreateBody();
            Rigidbody kept = CreateBody();
            EnemyController keptEnemy = CreateEnemy();
            _registry.Register(removed, CreateEnemy());
            _registry.Register(kept, keptEnemy);

            _registry.Unregister(removed);

            bool resolved = _registry.TryResolve(kept, out EnemyController result);
            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(keptEnemy));
        }

        [Test]
        public void Unregister_UnknownBody_DoesNotThrow()
        {
            Rigidbody body = CreateBody();

            Assert.That(() => _registry.Unregister(body), Throws.Nothing);
        }

        [Test]
        public void Unregister_NullBody_DoesNotThrow()
        {
            Assert.That(() => _registry.Unregister(null), Throws.Nothing);
        }

        private Rigidbody CreateBody()
        {
            GameObject host = new GameObject("Body");
            _spawned.Add(host);

            return host.AddComponent<Rigidbody>();
        }

        private EnemyController CreateEnemy()
        {
            GameObject host = new GameObject("Enemy");
            _spawned.Add(host);

            return host.AddComponent<EnemyController>();
        }
    }
}
