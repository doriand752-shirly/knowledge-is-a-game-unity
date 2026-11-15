using UnityEngine;
using UnityEngine.EventSystems;

namespace Kiag
{
    /// <summary>
    /// Empty-scene entry: builds clay stage + networking. Drop on any empty GameObject or use as only scene script.
    /// </summary>
    public class ShowBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindFirstObjectByType<ShowBootstrap>() != null) return;
            if (FindFirstObjectByType<HostStageController>() != null) return;
            var go = new GameObject("KiagBootstrap");
            go.AddComponent<ShowBootstrap>();
        }

        void Awake()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                // Old Input Manager (PlayerSettings activeInputHandler=0)
                es.AddComponent<StandaloneInputModule>();
            }

            if (FindFirstObjectByType<ShowStageBuilder>() == null)
            {
                var stage = new GameObject("ShowStage");
                stage.AddComponent<ShowStageBuilder>();
            }

            var net = GameObject.Find("KiagNet");
            if (net == null) net = new GameObject("KiagNet");
            if (net.GetComponent<ColyseusHostClient>() == null)
                net.AddComponent<ColyseusHostClient>();
            if (net.GetComponent<ColyseusSdkAdapter>() == null)
                net.AddComponent<ColyseusSdkAdapter>();

            if (FindFirstObjectByType<HostStageController>() == null)
            {
                var host = new GameObject("HostStage");
                var ctrl = host.AddComponent<HostStageController>();
                // client wired in HostStageController.Start
            }

            // Link client.stage
            var client = FindFirstObjectByType<ColyseusHostClient>();
            var stageCtrl = FindFirstObjectByType<HostStageController>();
            if (client != null && stageCtrl != null)
            {
                var so = new SerializedFieldHelper();
                so.Bind(client, stageCtrl);
            }
        }

        /// <summary>Tiny helper to assign private SerializeField via reflection.</summary>
        class SerializedFieldHelper
        {
            public void Bind(ColyseusHostClient client, HostStageController stage)
            {
                var field = typeof(ColyseusHostClient).GetField("stage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(client, stage);
                client.SnapshotReceived -= stage.ApplySnapshot;
                client.SnapshotReceived += stage.ApplySnapshot;
            }
        }
    }
}
