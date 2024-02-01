using System.Collections;
using UnityEngine;

public class DisableIfQuestLinkActive : MonoBehaviour
{
    void Start()
    {
        if (OVRPlugin.GetSystemHeadsetType() is OVRPlugin.SystemHeadset.Oculus_Link_Quest or OVRPlugin.SystemHeadset
                .Oculus_Link_Quest_2 or OVRPlugin.SystemHeadset.Meta_Link_Quest_Pro
            or OVRPlugin.SystemHeadset.Meta_Link_Quest_3)
        {
            OVRManager.HMDMounted += () =>
            {
                Debug.Log("HMD mounted, disabling simulator");
                gameObject.SetActive(false);
            };
            OVRManager.HMDUnmounted += () =>
            {
                Debug.Log("HMD unmounted, enabling simulator");
                gameObject.SetActive(true);
            };
        }
    }
}
