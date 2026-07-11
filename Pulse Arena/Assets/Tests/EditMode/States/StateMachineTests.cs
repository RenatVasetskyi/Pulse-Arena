using System.Collections.Generic;
using Architecture.States;
using Architecture.States.Interfaces;
using NUnit.Framework;

namespace PulseArena.Tests.EditMode.States
{
    /// <summary>
    ///     Tests <see cref="StateMachine" /> — the app-flow FSM that stores one state instance per concrete
    ///     type and, on <c>Enter&lt;TState&gt;()</c>, exits the active state before entering the next. The
    ///     machine keys states on the compile-time generic argument, so each fake must be a distinct concrete
    ///     type (NSubstitute proxies of <see cref="IState" /> share one runtime type and cannot model two
    ///     registered states); the fixture uses hand-rolled states that append Enter/Exit calls to a shared
    ///     log, letting tests assert exact call order.
    /// </summary>
    [TestFixture]
    public class StateMachineTests
    {
        private List<string> _log;
        private StateMachine _stateMachine;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _stateMachine = new StateMachine();
        }

        [Test]
        public void Enter_RegisteredState_CallsEnterOnThatState()
        {
            _stateMachine.AddState(new FirstState(_log));

            _stateMachine.Enter<FirstState>();

            Assert.That(_log, Is.EqualTo(new[] { "First.Enter" }));
        }

        [Test]
        public void Enter_AnotherStateActive_ExitsActiveStateBeforeEnteringNext()
        {
            _stateMachine.AddState(new FirstState(_log));
            _stateMachine.AddState(new SecondState(_log));
            _stateMachine.Enter<FirstState>();

            _stateMachine.Enter<SecondState>();

            Assert.That(_log, Is.EqualTo(new[] { "First.Enter", "First.Exit", "Second.Enter" }));
        }

        [Test]
        public void Enter_SameStateTypeAgain_ExitsAndReentersIt()
        {
            _stateMachine.AddState(new FirstState(_log));
            _stateMachine.Enter<FirstState>();

            _stateMachine.Enter<FirstState>();

            // Self-transition restarts the state; the machine does not special-case re-entry.
            Assert.That(_log, Is.EqualTo(new[] { "First.Enter", "First.Exit", "First.Enter" }));
        }

        [Test]
        public void AddState_SameStateTypeTwice_ThrowsInvalidOperationException()
        {
            _stateMachine.AddState(new FirstState(_log));

            Assert.That(() => _stateMachine.AddState(new FirstState(_log)), Throws.InvalidOperationException);
        }

        [Test]
        public void Enter_UnregisteredStateType_ThrowsInvalidOperationException()
        {
            Assert.That(() => _stateMachine.Enter<FirstState>(), Throws.InvalidOperationException);
        }

        /// <summary>Test fake that appends its Enter/Exit calls to a shared log so tests can assert exact call order.</summary>
        private abstract class LoggingState : IState
        {
            private readonly List<string> _log;
            private readonly string _name;

            protected LoggingState(List<string> log, string name)
            {
                _log = log;
                _name = name;
            }

            public void Enter() => _log.Add(_name + ".Enter");

            public void Exit() => _log.Add(_name + ".Exit");
        }

        /// <summary>First distinct concrete state type — the machine keys registrations on the compile-time type.</summary>
        private sealed class FirstState : LoggingState
        {
            public FirstState(List<string> log) : base(log, "First") { }
        }

        /// <summary>Second distinct concrete state type used to exercise cross-state transitions.</summary>
        private sealed class SecondState : LoggingState
        {
            public SecondState(List<string> log) : base(log, "Second") { }
        }
    }
}
