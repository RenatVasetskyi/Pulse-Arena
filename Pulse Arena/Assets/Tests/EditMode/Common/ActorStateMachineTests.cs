using System.Collections.Generic;
using Game.Common.StateMachine;
using NUnit.Framework;

namespace PulseArena.Tests.EditMode.Common
{
    /// <summary>
    ///     Unit tests for <see cref="ActorStateMachine" /> — the per-actor FSM shared by the player and every
    ///     enemy. The machine only routes lifecycle calls (Enter/Exit/Tick/FixedTick) to the active state, so
    ///     the tests drive it with a hand-rolled recording state and pin the routing contract: exit-before-enter
    ///     ordering on transitions, forwarding of ticks, and null-safety when no state is active.
    /// </summary>
    [TestFixture]
    public class ActorStateMachineTests
    {
        private ActorStateMachine _machine;
        private List<string> _log;

        [SetUp]
        public void CreateMachine()
        {
            _machine = new ActorStateMachine();
            _log = new List<string>();
        }

        [Test]
        public void ChangeState_EntersTheNewState_AndExposesItAsActive()
        {
            RecordingState state = new RecordingState("A", _log);

            _machine.ChangeState(state);

            Assert.That(_log, Is.EqualTo(new[] { "A:Enter" }));
            Assert.That(_machine.ActiveState, Is.SameAs(state));
        }

        [Test]
        public void ChangeState_ExitsPreviousState_BeforeEnteringNext()
        {
            RecordingState first = new RecordingState("A", _log);
            RecordingState second = new RecordingState("B", _log);
            _machine.ChangeState(first);
            _log.Clear();

            _machine.ChangeState(second);

            // Order is load-bearing: a state must finish tearing down before the next one arms itself.
            Assert.That(_log, Is.EqualTo(new[] { "A:Exit", "B:Enter" }));
        }

        [Test]
        public void Tick_ForwardsToActiveState()
        {
            RecordingState state = new RecordingState("A", _log);
            _machine.ChangeState(state);
            _log.Clear();

            _machine.Tick();

            Assert.That(_log, Is.EqualTo(new[] { "A:Tick" }));
        }

        [Test]
        public void FixedTick_ForwardsToActiveState()
        {
            RecordingState state = new RecordingState("A", _log);
            _machine.ChangeState(state);
            _log.Clear();

            _machine.FixedTick();

            Assert.That(_log, Is.EqualTo(new[] { "A:FixedTick" }));
        }

        [Test]
        public void Clear_ExitsActiveState_AndLeavesNoActiveState()
        {
            RecordingState state = new RecordingState("A", _log);
            _machine.ChangeState(state);
            _log.Clear();

            _machine.Clear();

            Assert.That(_log, Is.EqualTo(new[] { "A:Exit" }));
            Assert.That(_machine.ActiveState, Is.Null);
        }

        [Test]
        public void TickAndClear_WithNoActiveState_DoNotThrow()
        {
            // A pooled actor ticks before its first state is armed — the machine must tolerate that.
            Assert.DoesNotThrow(() => _machine.Tick());
            Assert.DoesNotThrow(() => _machine.FixedTick());
            Assert.DoesNotThrow(() => _machine.Clear());
        }

        /// <summary>Recording test double: appends every lifecycle call to a shared ordered log.</summary>
        private sealed class RecordingState : ActorState
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public override void Enter() => _log.Add(_name + ":Enter");
            public override void Exit() => _log.Add(_name + ":Exit");
            public override void Tick() => _log.Add(_name + ":Tick");
            public override void FixedTick() => _log.Add(_name + ":FixedTick");
        }
    }
}
