using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using System.Reflection;

// based on https://forum.unity.com/threads/set-a-default-positions-to-xr-device-simulator.1058912/#post-8401596

[RequireComponent(typeof(XRDeviceSimulator))]
public class XRDeviceSimulatorUsePosition : MonoBehaviour
{
    private void Start()
    {
        var deviceSimulator = GetComponent<XRDeviceSimulator>();

        var hmdFieldInfo =
            typeof(XRDeviceSimulator).GetField("m_HMDState", BindingFlags.NonPublic | BindingFlags.Instance);
        var leftFieldInfo =
            typeof(XRDeviceSimulator).GetField("m_LeftControllerState", BindingFlags.NonPublic | BindingFlags.Instance);
        var rightFieldInfo =
            typeof(XRDeviceSimulator).GetField("m_RightControllerState",
                BindingFlags.NonPublic | BindingFlags.Instance);

        var hmdState = hmdFieldInfo?.GetValue(deviceSimulator) as XRSimulatedHMDState?;
        var leftControllerState = leftFieldInfo?.GetValue(deviceSimulator) as XRSimulatedControllerState?;
        var rightControllerState = rightFieldInfo?.GetValue(deviceSimulator) as XRSimulatedControllerState?;

        if (!hmdState.HasValue || !leftControllerState.HasValue || !rightControllerState.HasValue) return;

        var hmd = hmdState.Value;
        var leftController = leftControllerState.Value;
        var rightController = rightControllerState.Value;

        var posDiff = transform.position - hmd.devicePosition;

        hmd.devicePosition += posDiff;
        hmd.centerEyePosition += posDiff;
        leftController.devicePosition += posDiff;
        rightController.devicePosition += posDiff;

        hmdFieldInfo.SetValue(deviceSimulator, hmd);
        leftFieldInfo.SetValue(deviceSimulator, leftController);
        rightFieldInfo.SetValue(deviceSimulator, rightController);
    }
}