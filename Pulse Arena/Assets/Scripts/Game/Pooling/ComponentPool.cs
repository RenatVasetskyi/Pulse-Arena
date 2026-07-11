using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Pooling
{
    /// <summary>
    ///     Minimal generic object pool for scene components. It only tracks membership (active set +
    ///     inactive queue) — ALL reset/activation behavior lives in the caller-supplied delegates:
    ///     <c>createFunc</c> builds a new instance when the pool is dry, <c>getAction</c> runs on checkout,
    ///     and <c>releaseAction</c> must fully reset state on return or stale state leaks across reuse.
    ///     Destroyed (Unity fake-null) instances left in the inactive queue are skipped on Get, so a torn-down
    ///     scene never hands out dead objects.
    /// </summary>
    public class ComponentPool<T> where T : Component
    {
        private readonly HashSet<T> _active = new();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _getAction;
        private readonly Queue<T> _inactive = new();
        private readonly Action<T> _releaseAction;

        public ComponentPool(
            Func<T> createFunc,
            Action<T> getAction,
            Action<T> releaseAction,
            int preloadCount)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _getAction = getAction;
            _releaseAction = releaseAction;

            Preload(preloadCount);
        }

        public T Get()
        {
            T item = GetInactiveOrCreate();

            _active.Add(item);
            _getAction?.Invoke(item);

            return item;
        }

        public void Release(T item)
        {
            if (item == null || !_active.Remove(item))
                return;

            _releaseAction?.Invoke(item);
            _inactive.Enqueue(item);
        }

        private void Preload(int preloadCount)
        {
            for (int i = 0; i < preloadCount; i++)
            {
                T item = _createFunc();
                _releaseAction?.Invoke(item);
                _inactive.Enqueue(item);
            }
        }

        private T GetInactiveOrCreate()
        {
            while (_inactive.Count > 0)
            {
                T item = _inactive.Dequeue();

                if (item != null)
                    return item;
            }

            return _createFunc();
        }
    }
}