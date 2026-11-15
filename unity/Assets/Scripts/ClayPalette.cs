using UnityEngine;

namespace Kiag
{
    /// <summary>
    /// Original QuiZène clay/figurine palette — soft PBR-ish mats + SSS-ish skin.
    /// Inspired by stop-motion TV quiz look (It's Quiz Time vibe), not Sony IP.
    /// </summary>
    public static class ClayPalette
    {
        public static readonly Color StageFloor = Hex("#F4D35E");
        public static readonly Color FloorStripe = Hex("#FFF8E7");
        public static readonly Color FloorGold = Hex("#E8B923");
        public static readonly Color Curtain = Hex("#C9184A");
        public static readonly Color BackWall = Hex("#1B3A4B");
        public static readonly Color BackdropSky = Hex("#6EB5E0");
        public static readonly Color BackdropFar = Hex("#3D7BA8");
        public static readonly Color BackdropCutout = Hex("#2A5F8A");
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
        public static readonly Color BadgeInk = Hex("#1A1A2E");

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

        static Shader FindLit()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                   ?? Shader.Find("Standard")
                   ?? Shader.Find("Diffuse");
        }

        /// <summary>Matte clay / foam figurine body — soft specular, low metal.</summary>
        public static Material MakeClayMat(Color color, float smoothness = 0.28f)
        {
            var mat = new Material(FindLit());
            ApplyColor(mat, color);
            ApplyPbr(mat, metallic: 0.02f, smoothness: smoothness);
            // Slight self-illum so clay never goes pitch-black in soft GI
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.04f);
            }
            return mat;
        }

        /// <summary>SSS-ish skin: warmer, softer gloss, gentle subsurface glow.</summary>
        public static Material MakeSkinMat(Color color, float smoothness = 0.42f)
        {
            var warm = Color.Lerp(color, Hex("#FFC4A8"), 0.22f);
            var mat = new Material(FindLit());
            ApplyColor(mat, warm);
            ApplyPbr(mat, metallic: 0f, smoothness: smoothness);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                // Fake SSS: warm under-glow
                mat.SetColor("_EmissionColor", Hex("#FF8F6B") * 0.08f + warm * 0.05f);
            }
            return mat;
        }

        /// <summary>Slightly shinier plastic / helmet / prop clay.</summary>
        public static Material MakePlasticMat(Color color, float smoothness = 0.55f)
        {
            var mat = new Material(FindLit());
            ApplyColor(mat, color);
            ApplyPbr(mat, metallic: 0.05f, smoothness: smoothness);
            return mat;
        }

        /// <summary>Soft gold glitter stripe / prop metal.</summary>
        public static Material MakeGoldMat(Color color, float smoothness = 0.72f)
        {
            var mat = new Material(FindLit());
            ApplyColor(mat, color);
            ApplyPbr(mat, metallic: 0.65f, smoothness: smoothness);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.12f);
            }
            return mat;
        }

        /// <summary>Flat stylized backdrop cutouts (less specular, cooler).</summary>
        public static Material MakeBackdropMat(Color color)
        {
            var mat = new Material(FindLit());
            ApplyColor(mat, color);
            ApplyPbr(mat, metallic: 0f, smoothness: 0.08f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.06f);
            }
            return mat;
        }

        static void ApplyColor(Material mat, Color color)
        {
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        static void ApplyPbr(Material mat, float metallic, float smoothness)
        {
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            // Soft shadows / diffuse wrap feel via lower specular intensity when available
            if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 1f);
        }
    }
}
