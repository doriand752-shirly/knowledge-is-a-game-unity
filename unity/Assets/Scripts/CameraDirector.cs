using UnityEngine;

namespace Kiag
{
    /// <summary>Broadcast-style camera cuts for theme doors / pads / pyramid.</summary>
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

        public void Bind(Camera broadcast, Camera door, Camera pad, Camera pyramid)
        {
            _broadcast = broadcast;
            _door = door;
            _pad = pad;
            _pyramid = pyramid;
            Cut("broadcast");
        }

        public void CutForPhase(string phase)
        {
            switch (phase)
            {
                case "theme_vote":
                case "theme_reveal":
                    Cut("door");
                    break;
                case "question":
                case "votes_show":
                case "reveal":
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
            SetCam(_broadcast, shot == "broadcast");
            SetCam(_door, shot == "door");
            SetCam(_pad, shot == "pad");
            SetCam(_pyramid, shot == "pyramid");
        }

        static void SetCam(Camera cam, bool on)
        {
            if (!cam) return;
            cam.enabled = on;
            var al = cam.GetComponent<AudioListener>();
            if (al) al.enabled = on;
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
            pos.y += Mathf.Sin(Time.time * 1.3f) * 0.03f;
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
