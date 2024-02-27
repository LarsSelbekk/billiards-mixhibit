using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

/**
 * This class is heavily based on XRBaseInteractable
 */
public class HoverOnlyInteractable : MonoBehaviour, IXRHoverInteractable
{
    #pragma warning disable CS0067
    public event Action<InteractableRegisteredEventArgs> registered;
    public event Action<InteractableUnregisteredEventArgs> unregistered;
    #pragma warning restore CS0067

    [field: SerializeField] public InteractionLayerMask interactionLayers { get; set; } = 1;
    [field: SerializeField] public List<Collider> colliders { get; set; }
    [field: SerializeField] public HoverEnterEvent firstHoverEntered { get; set; }
    [field: SerializeField] public HoverExitEvent lastHoverExited { get; set; }
    [field: SerializeField] public HoverEnterEvent hoverEntered { get; set; }
    [field: SerializeField] public HoverExitEvent hoverExited { get; set; }
    [FormerlySerializedAs("allowedTags")] public List<string> allowedInteractorTags;
    public List<IXRHoverInteractor> interactorsHovering { get; } = new();

    public bool isHovered => interactorsHovering.Any();

    private XRInteractionManager _xrInteractionManager;

    private void Awake()
    {
        _xrInteractionManager = GameObject.FindWithTag("XRInteractionManager").GetComponent<XRInteractionManager>();
    }

    private void OnEnable()
    {
        _xrInteractionManager.RegisterInteractable(this);
    }

    private void OnDisable()
    {
        _xrInteractionManager.UnregisterInteractable(this);
    }

    public Transform GetAttachTransform(IXRInteractor interactor)
    {
        return transform;
    }

    public void OnRegistered(InteractableRegisteredEventArgs args)
    {
    }

    public void OnUnregistered(InteractableUnregisteredEventArgs args)
    {
    }

    public void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
    }

    // Lifted from XRBaseInteractable, but always using collider distance mode
    public virtual float GetDistanceSqrToInteractor(IXRInteractor interactor)
    {
        var interactorAttachTransform = interactor?.GetAttachTransform(this);
        if (interactorAttachTransform == null)
            return float.MaxValue;

        var interactorPosition = interactorAttachTransform.position;
        XRInteractableUtility.TryGetClosestCollider(this, interactorPosition, out var distanceInfo);
        return distanceInfo.distanceSqr;
    }

    public bool IsHoverableBy(IXRHoverInteractor interactor)
    {
        return !allowedInteractorTags.Any() || allowedInteractorTags.Contains(interactor.transform.parent.gameObject.tag);
    }

    public void OnHoverEntering(HoverEnterEventArgs args)
    {
        interactorsHovering.Add(args.interactorObject);
    }

    public void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Silence any haptic feedback on hover
        if (args.interactorObject is XRBaseControllerInteractor controller && controller.playHapticsOnHoverEntered)
        {
            controller.SendHapticImpulse(0, controller.hapticHoverEnterDuration);
        }

        if (interactorsHovering.Count == 1)
        {
            firstHoverEntered?.Invoke(args);
        }

        hoverEntered?.Invoke(args);
    }

    public void OnHoverExiting(HoverExitEventArgs args)
    {
        interactorsHovering.Remove(args.interactorObject);
    }

    public void OnHoverExited(HoverExitEventArgs args)
    {
        // Silence any haptic feedback on hover
        if (args.interactorObject is XRBaseControllerInteractor controller && controller.playHapticsOnHoverExited)
        {
            controller.SendHapticImpulse(0, controller.hapticHoverExitDuration);
        }

        if (interactorsHovering.Count == 0)
        {
            lastHoverExited?.Invoke(args);
        }

        hoverExited?.Invoke(args);
    }
}
