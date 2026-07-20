using Architecture.Services.Interfaces;
using Data;
using Game.Cameras;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace SlingRing.Tests.EditMode.Cameras
{
    /// <summary>
    ///     Unit tests for <see cref="CameraZoomController" /> — the zoom axis that routes every zoom change
    ///     through <see cref="ISettingsService" /> so the on-screen +/- buttons and the settings slider share
    ///     one persisted value. The settings service is substituted (stubbed for reads, verified for writes),
    ///     which keeps PlayerPrefs completely out of the tests. The SmoothDamp easing in <c>Tick</c> depends on
    ///     <c>Time.unscaledDeltaTime</c> and is deliberately not asserted here — only the deterministic
    ///     clamping, scaling and settings-routing contracts are pinned.
    /// </summary>
    [TestFixture]
    public class CameraZoomControllerTests
    {
        private static readonly Vector3 BaseOffset = new Vector3(0f, 14f, -11f);

        private CameraZoomController _zoom;
        private ISettingsService _settings;
        private CameraData _data;

        [SetUp]
        public void CreateController()
        {
            _zoom = new CameraZoomController();
            _settings = Substitute.For<ISettingsService>();
            _data = new CameraData
            {
                DefaultZoom = 1f,
                MinZoom = 0.5f,
                MaxZoom = 2f,
                ZoomStep = 0.25f,
                ZoomSmoothTime = 0.2f,
            };
        }

        [Test]
        public void Initialize_ReadsPersistedZoom_AndScalesFollowOffset()
        {
            _settings.CameraZoom.Returns(1.5f); // stub: the persisted slider value

            _zoom.Initialize(_settings, BaseOffset, _data);

            Assert.That(_zoom.ZoomedFollowOffset.y, Is.EqualTo(BaseOffset.y * 1.5f).Within(1e-4f));
        }

        [Test]
        public void Initialize_ClampsPersistedZoomIntoConfiguredRange()
        {
            _settings.CameraZoom.Returns(99f); // corrupted / out-of-range pref must not explode the rig

            _zoom.Initialize(_settings, BaseOffset, _data);

            Assert.That(_zoom.ZoomedFollowOffset.y, Is.EqualTo(BaseOffset.y * _data.MaxZoom).Within(1e-4f));
        }

        [Test]
        public void Initialize_WithoutSettings_FallsBackToDefaultZoom()
        {
            _zoom.Initialize(null, BaseOffset, _data);

            Assert.That(_zoom.ZoomedFollowOffset.y, Is.EqualTo(BaseOffset.y * _data.DefaultZoom).Within(1e-4f));
        }

        [Test]
        public void ZoomIn_RoutesDecreasedTargetThroughSettings()
        {
            _settings.CameraZoom.Returns(1f);
            _zoom.Initialize(_settings, BaseOffset, _data);

            _zoom.ZoomIn();

            // The controller never applies the zoom directly — persistence is the single source of truth.
            _settings.Received(1).SetCameraZoom(0.75f);
        }

        [Test]
        public void ZoomOut_RoutesIncreasedTargetThroughSettings()
        {
            _settings.CameraZoom.Returns(1f);
            _zoom.Initialize(_settings, BaseOffset, _data);

            _zoom.ZoomOut();

            _settings.Received(1).SetCameraZoom(1.25f);
        }
    }
}
