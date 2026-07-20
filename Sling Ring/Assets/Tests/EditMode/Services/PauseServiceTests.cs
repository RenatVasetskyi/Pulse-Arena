using Architecture.Services;
using Architecture.Services.Interfaces;
using NSubstitute;
using NUnit.Framework;

namespace SlingRing.Tests.EditMode.Services
{
    /// <summary>
    ///     Verifies <see cref="PauseService" /> — the mechanical-pause registry that broadcasts
    ///     <see cref="IPausable.Pause" />/<see cref="IPausable.Resume" /> to registered gameplay systems
    ///     as a group. Approach: NSubstitute pausables assert broadcast counts (idempotency, registry
    ///     dedupe, unregister isolation, the register-while-paused auto-freeze, the <c>Clear</c> safety
    ///     net) while <c>IsPaused</c> is asserted to track the group state.
    /// </summary>
    [TestFixture]
    public class PauseServiceTests
    {
        private IPausable _pausable;
        private PauseService _pauseService;

        [SetUp]
        public void SetUp()
        {
            _pauseService = new PauseService();
            _pausable = Substitute.For<IPausable>();
        }

        [Test]
        public void Pause_CallsPauseOnRegisteredPausable()
        {
            _pauseService.Register(_pausable);

            _pauseService.Pause();

            _pausable.Received().Pause();
        }

        [Test]
        public void Pause_CalledTwice_BroadcastsPauseOnlyOnce()
        {
            _pauseService.Register(_pausable);

            _pauseService.Pause();
            _pauseService.Pause();

            _pausable.Received(1).Pause();
        }

        [Test]
        public void Unpause_AfterPause_CallsResumeOnRegisteredPausable()
        {
            _pauseService.Register(_pausable);
            _pauseService.Pause();

            _pauseService.Unpause();

            _pausable.Received(1).Resume();
        }

        [Test]
        public void Unpause_WithoutPriorPause_DoesNotBroadcastResume()
        {
            _pauseService.Register(_pausable);

            _pauseService.Unpause();

            _pausable.DidNotReceive().Resume();
        }

        [Test]
        public void Register_WhileAlreadyPaused_PausesNewcomerImmediately()
        {
            _pauseService.Pause();

            _pauseService.Register(_pausable);

            _pausable.Received(1).Pause();
        }

        [Test]
        public void Register_SamePausableTwice_PauseBroadcastsOnlyOnce()
        {
            _pauseService.Register(_pausable);
            _pauseService.Register(_pausable);

            _pauseService.Pause();

            _pausable.Received(1).Pause();
        }

        [Test]
        public void Register_NullThenPause_DoesNotThrow()
        {
            Assert.That(() =>
            {
                _pauseService.Register(null);
                _pauseService.Pause();
            }, Throws.Nothing);
        }

        [Test]
        public void Unregister_ExcludesPausableFromPauseBroadcast()
        {
            _pauseService.Register(_pausable);
            _pauseService.Unregister(_pausable);

            _pauseService.Pause();

            _pausable.DidNotReceive().Pause();
        }

        [Test]
        public void Clear_WhilePaused_ResumesRegisteredPausables()
        {
            _pauseService.Register(_pausable);
            _pauseService.Pause();

            _pauseService.Clear();

            _pausable.Received(1).Resume();
        }

        [Test]
        public void Clear_WhilePaused_SetsIsPausedFalse()
        {
            _pauseService.Register(_pausable);
            _pauseService.Pause();

            _pauseService.Clear();

            Assert.That(_pauseService.IsPaused, Is.False);
        }

        [Test]
        public void Clear_ThenPause_DoesNotReachClearedPausable()
        {
            _pauseService.Register(_pausable);
            _pauseService.Clear();

            _pauseService.Pause();

            _pausable.DidNotReceive().Pause();
        }

        [Test]
        public void IsPaused_BeforeAnyCall_IsFalse()
        {
            Assert.That(_pauseService.IsPaused, Is.False);
        }

        [Test]
        public void Pause_SetsIsPausedTrue()
        {
            _pauseService.Pause();

            Assert.That(_pauseService.IsPaused, Is.True);
        }

        [Test]
        public void Unpause_AfterPause_SetsIsPausedFalse()
        {
            _pauseService.Pause();

            _pauseService.Unpause();

            Assert.That(_pauseService.IsPaused, Is.False);
        }
    }
}
