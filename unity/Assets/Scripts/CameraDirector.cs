using UnityEngine;

namespace Kiag
{
    /// <summary>Broadcast-style camera cuts; keeps ClayPostFx focus matched to shot.</summary>
    public class CameraDirector : MonoBehaviour
    {
        Camera _broadcast;
        Camera _door;
        Camera _pad;
        Camera _pyramid;
        string _shot = "broadcast";
        float _shake;
        Vector3 _basePos;
        bool _baseSet;
        bool _allowBob;

        public void Bind(Camera broadcast, Camera door, Camera pad, Camera pyramid)
        {
            _broadcast = broadcast;
            _door = door;
            _pad = pad;
            _pyramid = pyramid;
            Cut("broadcast");
            _allowBob = false;
        }

        public void CutForPhase(string phase)
        {
            _allowBob = phase != null && phase != "lobby" && phase != "countdown";
            switch (phase)
            {
                case "theme_vote":
                case "theme_reveal":
                    Cut("door");
                    break;
                case "question":
                case "votes_show":
                case "reveal":
                case "linking":
                case "sorting":
                case "chain_reaction":
                case "fill_blanks":
                case "odd_one_out":
                case "spin_to_win":
                    Cut("pad");
                    break;
                case "pyramid":
                    Cut("pyramid");
                    break;
                case "power_play":
                    Cut("broadcast");
                    _shake = 0.35f;
                    break;
                default:
                    Cut("broadcast");
                    break;
            }
        }

        public void Cut(string shot)
        {
            _shot = shot;
            _baseSet = false;
            SetCam(_broadcast, shot == "broadcast", 4.8f);
            SetCam(_door, shot == "door", 4.2f);
            SetCam(_pad, shot == "pad", 4.0f);
            SetCam(_pyramid, shot == "pyramid", 5.0f);
        }

        static void SetCam(Camera cam, bool on, float focus)
        {
            if (!cam) return;
            cam.enabled = on;
            var al = cam.GetComponent<AudioListener>();
            if (al) al.enabled = on;
            var fx = cam.GetComponent<ClayPostFx>();
            if (fx && on) fx.SetFocus(focus);
        }

        void LateUpdate()
        {
            var active = ActiveCam();
            if (!active) return;
            if (!_baseSet)
            {
                _basePos = active.transform.position;
                _baseSet = true;
            }
            var pos = _basePos;
            if (_shake > 0)
            {
                _shake -= Time.deltaTime;
                pos += Random.insideUnitSphere * 0.04f;
            }
            if (_allowBob)
                pos.y += Mathf.Sin(Time.time * 1.3f) * 0.02f;
            active.transform.position = pos;
        }

        Camera ActiveCam()
        {
            return _shot switch
            {
                "door" => _door,
                "pad" => _pad,
                "pyramid" => _pyramid,
                _ => _broadcast
            };
        }
    }
}
