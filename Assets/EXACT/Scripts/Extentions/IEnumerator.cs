using UnityEngine;

using System.Collections;

public static class IEnumeratorExtension
{
    public static void StartCoroutineSingle(this MonoBehaviour mono, IEnumerator newInstance, ref IEnumerator instance)
    {
        if (instance != null)
        {
            mono.StopCoroutine(instance);
        }
        mono.StartCoroutine(newInstance);
        instance = newInstance;
    }

    public static void StopCoroutineSingle(this MonoBehaviour mono, IEnumerator instance)
    {
        if (instance != null)
        {
            mono.StopCoroutine(instance);
        }
    }
}
