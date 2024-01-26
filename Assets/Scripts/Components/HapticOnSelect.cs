using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticOnSelect : MonoBehaviour
{
    public void SendHapticImpulseForSelectEnter(SelectEnterEventArgs args)
    {
        args.interactorObject.transform.GetComponentInParent<ActionBasedController>()?.SendHapticImpulse(0.69f, 0.25f);
    }
}