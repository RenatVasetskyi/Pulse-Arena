using System;
using Architecture.Services.Interfaces;
using TMPro;
using UI.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    /// <summary>
    /// Self-contained settings panel (its own overlay canvas). Two-way binds sliders/toggles to
    /// <see cref="ISettingsService"/>. Reused from the main menu and the in-game pause. Assign the
    /// controls on the prefab.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _window;
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _zoomSlider;
        [SerializeField] private Toggle _shakeToggle;
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Button _closeButton;

        [Header("Optional value labels")]
        [SerializeField] private TextMeshProUGUI _masterValue;
        [SerializeField] private TextMeshProUGUI _musicValue;
        [SerializeField] private TextMeshProUGUI _sfxValue;
        [SerializeField] private TextMeshProUGUI _zoomValue;

        private ISettingsService _settings;
        private Vector3 _windowBaseScale = Vector3.one;
        private bool _suppress;

        public event Action Closed;

        public void Bind(ISettingsService settings, float zoomMin, float zoomMax)
        {
            _settings = settings;

            if (_window != null)
                _windowBaseScale = _window.localScale;

            if (_zoomSlider != null)
            {
                _zoomSlider.minValue = zoomMin;
                _zoomSlider.maxValue = zoomMax;
            }

            AddSlider(_masterSlider, v => _settings.SetMasterVolume(v));
            AddSlider(_musicSlider, v => _settings.SetMusicVolume(v));
            AddSlider(_sfxSlider, v => _settings.SetSfxVolume(v));
            AddSlider(_zoomSlider, v => _settings.SetCameraZoom(v));
            AddToggle(_shakeToggle, on => _settings.SetCameraEffectsEnabled(on));
            AddToggle(_vibrationToggle, on => _settings.SetVibrationEnabled(on));

            if (_closeButton != null)
                _closeButton.onClick.AddListener(RaiseClosed);

            Hide();
        }

        public void Show()
        {
            // Activate first, THEN push values — setting a slider while its object is inactive doesn't
            // stick and gets re-derived from the handle on enable.
            gameObject.SetActive(true);
            RefreshFromSettings();

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            UiTween.OpenWindow(_window, _canvasGroup, _windowBaseScale);
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private void RaiseClosed()
        {
            Hide();
            Closed?.Invoke();
        }

        private void AddSlider(Slider slider, Action<float> apply)
        {
            if (slider == null)
                return;

            slider.onValueChanged.AddListener(value =>
            {
                if (_suppress)
                    return;

                apply(value);
                UpdateValueLabels();
            });
        }

        private void AddToggle(Toggle toggle, Action<bool> apply)
        {
            if (toggle == null)
                return;

            toggle.onValueChanged.AddListener(value =>
            {
                if (!_suppress)
                    apply(value);
            });
        }

        private void RefreshFromSettings()
        {
            if (_settings == null)
                return;

            _suppress = true;

            if (_masterSlider != null) _masterSlider.value = _settings.MasterVolume;
            if (_musicSlider != null) _musicSlider.value = _settings.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = _settings.SfxVolume;
            if (_zoomSlider != null) _zoomSlider.value = _settings.CameraZoom;
            if (_shakeToggle != null) _shakeToggle.isOn = _settings.CameraEffectsEnabled;
            if (_vibrationToggle != null) _vibrationToggle.isOn = _settings.VibrationEnabled;

            _suppress = false;
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            if (_masterValue != null && _masterSlider != null) _masterValue.text = Percent(_masterSlider.value);
            if (_musicValue != null && _musicSlider != null) _musicValue.text = Percent(_musicSlider.value);
            if (_sfxValue != null && _sfxSlider != null) _sfxValue.text = Percent(_sfxSlider.value);
            if (_zoomValue != null && _zoomSlider != null) _zoomValue.text = _zoomSlider.value.ToString("0.00") + "x";
        }

        private static string Percent(float value01)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f) + "%";
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
