using System.Collections.Generic;
using UnityEngine;

public class HmdOnlyEnforcer : MonoBehaviour
{
    public List<MonoBehaviour> subjects;
    
    private void Start()
    {
        if (OVRManager.isHmdPresent) return;
        foreach (var s in subjects)
        {
            s.enabled = false;
        }
    }
}
