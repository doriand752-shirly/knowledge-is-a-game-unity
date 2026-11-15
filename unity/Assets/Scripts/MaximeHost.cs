using UnityEngine;

namespace Kiag
{
    /// <summary>Clay host puppet — talk / cheer / idle (original Maxime, not Sony Max).</summary>
    public class MaximeHost : MonoBehaviour
    {
        string _mood = "idle";
        float _t;

        public void SetMood(string mood)
        {
            _mood = string.IsNullOrEmpty(mood) ? "idle" : mood;
        }

        void Update()
        {
            _t += Time.deltaTime;
            // Stepped clay feel ~12fps pose sampling
            if (Mathf.FloorToInt(_t * 12f) == Mathf.FloorToInt((_t - Time.deltaTime) * 12f))
                return;

            var body = transform;
            switch (_mood)
            {
                case "talk":
                case "question":
                    body.localRotation = Quaternion.Euler(0, Mathf.Sin(_t * 6f) * 8f, 0);
                    body.localScale = new Vector3(1f, 1f + 0.03f * Mathf.Sin(_t * 8f), 1f);
                    break;
                case "cheer":
                case "reveal":
                    body.localRotation = Quaternion.Euler(0, Mathf.Sin(_t * 10f) * 12f, 0);
                    body.localScale = Vector3.one * (1f + 0.06f * Mathf.Abs(Mathf.Sin(_t * 9f)));
                    break;
                case "suspense":
                case "votes_show":
                    body.localRotation = Quaternion.Euler(0, Mathf.Sin(_t * 2f) * 4f, 0);
                    break;
                default:
                    body.localRotation = Quaternion.Euler(0, Mathf.Sin(_t * 1.5f) * 3f, 0);
                    body.localScale = Vector3.one;
                    break;
            }
        }
    }
}
