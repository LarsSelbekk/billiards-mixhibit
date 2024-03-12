# Billiards Mixhibit

This is the Master's project of Mathias Oterhals Myklebust and Lars Mitsem Selbekk,
Computer Science students at the Norwegian University of Science and Technology (NTNU) in Trondheim.
It is a billiards Mixed Reality exhibit for the science center [Vitensenteret i Trondheim](https://vitensenteret.com/),
allowing youths to explore Mixed Reality in a playful and social environment.

### Hacky modifications of [`UberPost.shader`](<Assets/XR Overrides/UberPost.shader>)
The UberPost-shader has been modified to fix passthrough with post-processing. If passthrough is completely black, select [`CustomPostProcessData.asset`](Assets/Settings/CustomPostProcessData.asset) in the Inspector, set the Inspector to debug mode using the ellipsis button in the upper right corner, and make sure the `Uber Post PS` shader reference is set to the one in `Assets/XR Overrides/UberPost.shader`. The script is modified as described [here](https://communityforums.atmeta.com/t5/Unity-VR-Development/Passthrough-and-post-processing/m-p/1131527/highlight/true#M23353).

### Hacky modifications of [`XRGrabInteractable.cs`](Packages/com.unity.xr.interaction.toolkit@2.5.2/Runtime/Interaction/Interactables/XRGrabInteractable.cs)

The [`XRGrabInteractable.cs`](Packages/com.unity.xr.interaction.toolkit@2.5.2/Runtime/Interaction/Interactables/XRGrabInteractable.cs) from XR Interaction Toolkit has been modified to prevent reparenting of grabbed objects to root (as suggested [here](https://gamedev.stackexchange.com/a/198143)). The reparenting causes problems with world locking. Since the package is modified locally, it has been moved from [`Library/PackageCache`](Library/PackageCache) to [`Packages`](Packages) under version control (as suggested [here](https://forum.unity.com/threads/how-to-locally-modify-source-code-in-a-package.1445890/#post-9064735)). This means that updates must be performed manually!

### ~~`XR Grab Interactable` as `NetworkObject`~~

~~Since `XR Grab Interactable` is moved to root on grab, and only the server can reparent a `Network Object`, the `Auto Object Parent Sync` option on `Network Object` should be disabled. The alternative would be to [modify XRGrabInteractable.cs](https://gamedev.stackexchange.com/a/198143), which presumably breaks other stuff in non-obvious ways.~~


### [LocalPackages](LocalPackages)

The packages `com.meta.xr.mrutilitykit-60.0.0` and `com.meta.xr.sdk.interaction-60.0.0` have been added from custom tarballs, to work around a [known issue](https://developer.oculus.com/downloads/package/meta-xr-mr-utility-kit-upm/#known-issues) concerning the dependency on TextMeshPro combined with Unity v2023.2 and above. These modified packages should be replaced with the proper packages from the Unity registry once a fix has been published.
