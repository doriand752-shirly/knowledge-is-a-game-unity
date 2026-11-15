using TMPro;
using UnityEngine;

namespace Kiag
{
    /// <summary>
    /// Ensures every runtime TMP label gets LiberationSans SDF (Essential Resources).
    /// Without this, TextMeshProUGUI has a null font and lobby/button text is invisible.
    /// </summary>
    public static class KiagTmp
    {
        static TMP_FontAsset _font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (_font == null && TMP_Settings.defaultFontAsset != null)
                    _font = TMP_Settings.defaultFontAsset;
                if (_font == null)
                {
                    var os = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 36);
                    if (os != null)
                        _font = TMP_FontAsset.CreateFontAsset(os);
                }
                return _font;
            }
        }

        public static void Bind(TMP_Text tmp, Color color, bool outline = true)
        {
            if (tmp == null) return;
            var font = Font;
            if (font != null) tmp.font = font;
            tmp.color = color;
            tmp.enableVertexGradient = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            if (outline)
            {
                tmp.outlineWidth = 0.22f;
                tmp.outlineColor = new Color(0f, 0f, 0f, 0.92f);
            }
            else
            {
                tmp.outlineWidth = 0f;
            }
        }
    }
}
