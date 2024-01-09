# Billiards Mixhibit

This is the Master's project of Mathias Oterhals Myklebust and Lars Mitsem Selbekk,
Computer Science students at the Norwegian University of Science and Technology (NTNU) in Trondheim.
It is a billiards Mixed Reality exhibit for the science center [Vitensenteret i Trondheim](https://vitensenteret.com/),
allowing youths to explore Mixed Reality in a playful and social environment.


### [LocalPackages](LocalPackages)

The packages `com.meta.xr.mrutilitykit-60.0.0` and `com.meta.xr.sdk.interaction-60.0.0` have been added from custom tarballs, to work around a [known issue](https://developer.oculus.com/downloads/package/meta-xr-mr-utility-kit-upm/#known-issues) concerning the dependency on TextMeshPro combined with Unity v2023.2 and above. These modified packages should be replaced with the proper packages from the Unity registry once a fix has been published.