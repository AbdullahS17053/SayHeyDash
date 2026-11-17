using System;
using System.Collections;
using System.Text;
using SayHeyDash.Controls;
using SayHeyDash.Platform;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace SayHeyDash.Prototype
{
    /// <summary>
    /// PROTOTYPE (Milestone 1 tech PoC).
    /// Shows FPS, resolution, iframe/focus/touch state and last input device, refreshed four
    /// times per second (coroutine, no per-frame string allocations beyond the FPS sample).
    /// Used to collect the WebGL / Wix feasibility evidence.
    /// </summary>
    public sealed class PrototypeDiagnosticsPanel : MonoBehaviour
    {
        [SerializeField] private Text output;
        [SerializeField] private GameInput input;
        [SerializeField] private PrototypeActionFeedback actionFeedback;
        [SerializeField, Min(0.1f)] private float refreshSeconds = 0.25f;

        private readonly StringBuilder _sb = new StringBuilder(256);
        private IDisposable _anyButtonListener;
        private string _lastDevice = "-";
        private int _frames;
        private float _elapsed;
        private float _fps;

        private void OnEnable()
        {
            _anyButtonListener = InputSystem.onAnyButtonPress.Call(c => _lastDevice = c.device.displayName);
            StartCoroutine(RefreshLoop());
        }

        private void OnDisable()
        {
            _anyButtonListener?.Dispose();
            _anyButtonListener = null;
        }

        private void Update()
        {
            // FPS sampling needs per-frame data; this is the only per-frame work here.
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
        }

        private IEnumerator RefreshLoop()
        {
            var wait = new WaitForSecondsRealtime(refreshSeconds);
            while (enabled)
            {
                yield return wait;
                if (_elapsed > 0f) _fps = _frames / _elapsed;
                _frames = 0;
                _elapsed = 0f;
                Render();
            }
        }

        private void Render()
        {
            if (output == null) return;

            Vector2 move = input != null ? input.Move : Vector2.zero;
            _sb.Clear()
               .Append("FPS ").Append(_fps.ToString("0.0"))
               .Append("  (").Append(_fps > 0f ? (1000f / _fps).ToString("0.0") : "-").Append(" ms)\n")
               .Append("SCREEN ").Append(Screen.width).Append('x').Append(Screen.height)
               .Append("  DPR ").Append(BrowserBridge.DevicePixelRatio.ToString("0.00")).Append('\n')
               .Append("WEB PLAYER ").Append(YesNo(BrowserBridge.IsWebPlayer))
               .Append("  IFRAME ").Append(YesNo(BrowserBridge.IsEmbedded)).Append('\n')
               .Append("PAGE FOCUS ").Append(YesNo(BrowserBridge.HasFocus))
               .Append("  TOUCH ").Append(YesNo(BrowserBridge.IsTouchDevice)).Append('\n')
               .Append("LAST DEVICE ").Append(_lastDevice).Append('\n')
               .Append("MOVE ").Append(move.x.ToString("0.0")).Append(", ").Append(move.y.ToString("0.0"))
               .Append("  ACTIONS ").Append(actionFeedback != null ? actionFeedback.ActionCount : 0).Append('\n')
               .Append("AUDIO ").Append(AudioListener.volume > 0f ? "ON" : "MUTED");
            output.text = _sb.ToString();
        }

        private static string YesNo(bool value) => value ? "YES" : "NO";
    }
}
