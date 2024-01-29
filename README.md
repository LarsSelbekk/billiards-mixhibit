# Billiards Mixhibit

This is the Master's project of Mathias Oterhals Myklebust and Lars Mitsem Selbekk,
Computer Science students at the Norwegian University of Science and Technology (NTNU) in Trondheim.
It is a billiards Mixed Reality exhibit for the science center [Vitensenteret i Trondheim](https://vitensenteret.com/),
allowing youths to explore Mixed Reality in a playful and social environment.


### `XR Grab Interactable` as `NetworkObject`

Since `XR Grab Interactable` is moved to root on grab, and only the server can reparent a `Network Object`, the `Auto Object Parent Sync` option on `Network Object` should be disabled. The alternative would be to [modify XRGrabInteractable.cs](https://gamedev.stackexchange.com/a/198143), which presumably breaks other stuff in non-obvious ways.


### [LocalPackages](LocalPackages)

The packages `com.meta.xr.mrutilitykit-60.0.0` and `com.meta.xr.sdk.interaction-60.0.0` have been added from custom tarballs, to work around a [known issue](https://developer.oculus.com/downloads/package/meta-xr-mr-utility-kit-upm/#known-issues) concerning the dependency on TextMeshPro combined with Unity v2023.2 and above. These modified packages should be replaced with the proper packages from the Unity registry once a fix has been published.


## Resources

- **World Locking Tools documentation**
  https://learn.microsoft.com/en-us/mixed-reality/world-locking-tools/

- **Minimal World Locking Tools (WLT) setup for a world-locked application**
https://microsoft.github.io/MixedReality-WorldLockingTools-Samples/Tutorial/01_Minimal/01_Minimal.html