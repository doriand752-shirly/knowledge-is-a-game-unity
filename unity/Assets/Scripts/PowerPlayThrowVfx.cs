using UnityEngine;

namespace Kiag
{
    /// <summary>3D projectile from attacker to target for Power Play spectacle.</summary>
    public class PowerPlayThrowVfx : MonoBehaviour
    {
        Vector3 _from;
        Vector3 _to;
        float _life = 1.2f;
        float _age;
        Color _color = Color.green;
        TrailRenderer _trail;

        public static void Spawn(Transform root, Vector3 from, Vector3 to, string kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PP_" + kind;
            go.transform.SetParent(root, true);
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.35f;
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);

            var color = KindColor(kind);
            var rend = go.GetComponent<Renderer>();
            if (rend) rend.sharedMaterial = ClayPalette.MakeClayMat(color, 0.6f);

            var fx = go.AddComponent<PowerPlayThrowVfx>();
            fx._from = from;
            fx._to = to;
            fx._color = color;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Diffuse"));
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            fx._trail = trail;
        }

        static Color KindColor(string kind)
        {
            return kind switch
            {
                "freeze" => ClayPalette.Hex("#A8DADC"),
                "gloop" => ClayPalette.Hex("#1FA6A0"),
                "bombles" or "nibblers" => ClayPalette.Hex("#E76F51"),
                "zipper" => ClayPalette.Hex("#F4C430"),
                "disco_inferno" => ClayPalette.Hex("#9B5DE5"),
                "lockdown" => ClayPalette.Hex("#457B9D"),
                "millennium_bug" => ClayPalette.Hex("#8AC926"),
                _ => ClayPalette.NeonLime
            };
        }

        void Update()
        {
            _age += Time.deltaTime;
            float u = Mathf.Clamp01(_age / (_life * 0.7f));
            // Arc throw
            var mid = Vector3.Lerp(_from, _to, u);
            mid.y += Mathf.Sin(u * Mathf.PI) * 2.2f;
            transform.position = mid;
            transform.Rotate(0, 0, 720f * Time.deltaTime);

            if (_age >= _life)
            {
                // Splash
                var splash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                splash.transform.position = _to;
                splash.transform.localScale = Vector3.one * 0.8f;
                var c = splash.GetComponent<Collider>();
                if (c) Destroy(c);
                var r = splash.GetComponent<Renderer>();
                if (r) r.sharedMaterial = ClayPalette.MakeClayMat(_color, 0.2f);
                Destroy(splash, 0.45f);
                Destroy(gameObject);
            }
        }
    }
}
