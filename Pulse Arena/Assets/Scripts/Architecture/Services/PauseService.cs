using System.Collections.Generic;
using Architecture.Services.Interfaces;

namespace Architecture.Services
{
    /// <summary>
    ///     Mechanical pause with no Time.timeScale: keeps a registry of gameplay <see cref="IPausable" />s and
    ///     freezes / resumes them as a group. Each pausable caches + restores its own state, so a resume
    ///     continues from the exact spot (music sample, animation frame, FSM state, physics velocity) — nothing
    ///     restarts. Global (ProjectContext) so both parent-scope services (audio) and scene objects register;
    ///     scene pausables unregister on teardown / pool-return (<see cref="Clear" /> is the safety net).
    /// </summary>
    public class PauseService : IPauseService
    {
        private readonly HashSet<IPausable> _pausables = new();

        // Pause()/Resume() may cause a pausable to unregister mid-broadcast (e.g. a tween completing on
        // resume despawns an object) — always iterate a snapshot so the set isn't mutated while enumerated.
        private readonly List<IPausable> _snapshot = new();

        public bool IsPaused { get; private set; }

        public void Register(IPausable pausable)
        {
            if (pausable == null || !_pausables.Add(pausable))
                return;

            // Spawned/registered while already paused (e.g. a spawner coroutine we didn't gate in time) —
            // freeze it immediately so it never runs live under a pause.
            if (IsPaused)
                pausable.Pause();
        }

        public void Unregister(IPausable pausable)
        {
            if (pausable != null)
                _pausables.Remove(pausable);
        }

        public void Pause()
        {
            if (IsPaused)
                return;

            IsPaused = true;
            ForEachPausable(true);
        }

        public void Unpause()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            ForEachPausable(false);
        }

        public void Clear()
        {
            if (IsPaused)
                Unpause();

            _pausables.Clear();
        }

        private void ForEachPausable(bool pause)
        {
            _snapshot.Clear();
            _snapshot.AddRange(_pausables);

            foreach (IPausable pausable in _snapshot)
            {
                // A scene/pooled pausable destroyed without unregistering leaves a Unity fake-null here; skip it so a
                // stale reference can't throw MissingReferenceException mid-broadcast.
                if (pausable is UnityEngine.Object unityObject && unityObject == null)
                    continue;

                if (pause)
                    pausable.Pause();
                else
                    pausable.Resume();
            }
        }
    }
}
