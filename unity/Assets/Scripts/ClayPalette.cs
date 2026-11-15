using UnityEngine;

namespace Kiag
{
    /// <summary>Original QuiZène claymation palette — inspired by stop-motion TV quiz, not Sony IP.</summary>
    public static class ClayPalette
    {
        public static readonly Color StageFloor = Hex("#F2C94C");
        public static readonly Color Curtain = Hex("#C9184A");
        public static readonly Color BackWall = Hex("#1B3A4B");
        public static readonly Color NeonCyan = Hex("#5EC8E8");
        public static readonly Color NeonMagenta = Hex("#E6399B");
        public static readonly Color NeonLime = Hex("#8AC926");
        public static readonly Color PadA = Hex("#E63946");
        public static readonly Color PadB = Hex("#457B9D");
        public static readonly Color PadC = Hex("#2A9D8F");
        public static readonly Color PadD = Hex("#F4A261");
        public static readonly Color MaximeSuit = Hex("#2D6A4F");
        public static readonly Color MaximeSkin = Hex("#F4D6AE");
        public static readonly Color PyramidGold = Hex("#F4C430");
        public static readonly Color ClayWhite = Hex("#FFF6E8");

        public static readonly Color[] CastColors =
        {
            Hex("#E63946"), Hex("#9B5DE5"), Hex("#5EC8E8"), Hex("#A8DADC"),
            Hex("#F4A261"), Hex("#457B9D"), Hex("#6A4C93"), Hex("#2A9D8F"),
            Hex("#E76F51"), Hex("#ADB5BD"),
        };

        public static readonly string[] CastIds =
        {
            "ketchup", "glitter", "scintille", "yeti", "bonnie",
            "miette", "magus", "bizzarro", "volcan", "toast",
        };

        public static readonly string[] CastNames =
        {
            "Capitaine Ketchup", "Duchesse Glitter", "Fée Scintille", "Yéti Spa", "Bonnie Laser",
            "Major Miette", "Magus Oops", "Dr Bizzarro", "Chef Volcan", "T0AST-3000",
        };

        public static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var c)) return Color.magenta;
            return c;
        }

        public static Color ForCast(string characterId)
        {
            for (int i = 0; i < CastIds.Length; i++)
                if (CastIds[i] == characterId) return CastColors[i];
            return CastColors[0];
        }

        public static Material MakeClayMat(Color color, float smoothness = 0.35f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }
    }
}
