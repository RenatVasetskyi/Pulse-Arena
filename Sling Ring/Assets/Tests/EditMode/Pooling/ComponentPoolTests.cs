using System;
using System.Collections.Generic;
using Game.Pooling;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlingRing.Tests.EditMode.Pooling
{
    /// <summary>
    /// Tests for <see cref="ComponentPool{T}"/>, the membership-tracking pool whose reset logic
    /// lives entirely in caller-supplied delegates. Uses <see cref="Transform"/> as the pooled
    /// component: every created GameObject is tracked and destroyed in teardown, and create/get/release
    /// counters make delegate invocations and instance reuse observable without frames or physics.
    /// </summary>
    [TestFixture]
    public class ComponentPoolTests
    {
        private readonly List<GameObject> _createdObjects = new();

        private int _createCount;
        private int _getCount;
        private int _releaseCount;
        private Transform _lastGot;
        private Transform _lastReleased;

        [SetUp]
        public void SetUp()
        {
            _createCount = 0;
            _getCount = 0;
            _releaseCount = 0;
            _lastGot = null;
            _lastReleased = null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Constructor_NullCreateFunc_ThrowsArgumentNullException()
        {
            Assert.That(() => new ComponentPool<Transform>(null, null, null, 0),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_PreloadCount_CreatesThatManyInstancesUpFront()
        {
            CreatePool(3);

            Assert.That(_createCount, Is.EqualTo(3));
        }

        [Test]
        public void Get_EmptyPool_CreatesInstanceViaCreateFunc()
        {
            ComponentPool<Transform> pool = CreatePool(0);

            Transform item = pool.Get();

            Assert.That(_createCount, Is.EqualTo(1));
            Assert.That(_createdObjects, Does.Contain(item.gameObject));
        }

        [Test]
        public void Get_InvokesGetActionWithReturnedItem()
        {
            ComponentPool<Transform> pool = CreatePool(0);

            Transform item = pool.Get();

            Assert.That(_getCount, Is.EqualTo(1));
            Assert.That(_lastGot, Is.SameAs(item));
        }

        [Test]
        public void Get_AfterPreload_ReturnsPreloadedInstanceWithoutNewCreate()
        {
            ComponentPool<Transform> pool = CreatePool(2);

            Transform item = pool.Get();

            Assert.That(_createCount, Is.EqualTo(2));
            Assert.That(_createdObjects, Does.Contain(item.gameObject));
        }

        [Test]
        public void Get_AfterRelease_ReusesSameInstanceWithoutNewCreate()
        {
            ComponentPool<Transform> pool = CreatePool(0);
            Transform first = pool.Get();
            pool.Release(first);

            Transform second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(_createCount, Is.EqualTo(1));
        }

        [Test]
        public void Get_DestroyedInactiveItem_SkipsItAndReturnsLiveInstance()
        {
            ComponentPool<Transform> pool = CreatePool(0);
            Transform destroyed = pool.Get();
            pool.Release(destroyed);
            Object.DestroyImmediate(destroyed.gameObject);

            Transform revived = pool.Get();

            // Unity fake-null: a destroyed Transform still passes reference checks,
            // so liveness must go through the engine's lifetime-aware operator.
            bool isAlive = revived != null;
            Assert.That(isAlive, Is.True);
            Assert.That(_createCount, Is.EqualTo(2));
        }

        [Test]
        public void Release_ActiveItem_InvokesReleaseActionWithThatItem()
        {
            ComponentPool<Transform> pool = CreatePool(0);
            Transform item = pool.Get();

            pool.Release(item);

            Assert.That(_releaseCount, Is.EqualTo(1));
            Assert.That(_lastReleased, Is.SameAs(item));
        }

        [Test]
        public void Release_ItemNotOwnedByPool_DoesNotInvokeReleaseAction()
        {
            ComponentPool<Transform> pool = CreatePool(0);
            Transform foreign = CreateForeignTransform();

            pool.Release(foreign);

            Assert.That(_releaseCount, Is.EqualTo(0));
        }

        [Test]
        public void Release_SameItemTwice_InvokesReleaseActionOnlyOnce()
        {
            ComponentPool<Transform> pool = CreatePool(0);
            Transform item = pool.Get();
            pool.Release(item);

            pool.Release(item);

            Assert.That(_releaseCount, Is.EqualTo(1));
        }

        private ComponentPool<Transform> CreatePool(int preloadCount)
        {
            return new ComponentPool<Transform>(
                CreateTrackedTransform,
                item =>
                {
                    _getCount++;
                    _lastGot = item;
                },
                item =>
                {
                    _releaseCount++;
                    _lastReleased = item;
                },
                preloadCount);
        }

        private Transform CreateTrackedTransform()
        {
            GameObject pooledObject = new GameObject("Pooled");
            _createdObjects.Add(pooledObject);
            _createCount++;

            return pooledObject.transform;
        }

        private Transform CreateForeignTransform()
        {
            GameObject foreignObject = new GameObject("Foreign");
            _createdObjects.Add(foreignObject);

            return foreignObject.transform;
        }
    }
}
