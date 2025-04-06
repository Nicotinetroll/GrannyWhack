using UnityEngine;
using System.Collections;

public class AutoDisableVFX : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        StartCoroutine(DisableWhenDone());
    }

    private IEnumerator DisableWhenDone()
    {
        yield return new WaitUntil(() => !ps.isPlaying);
        gameObject.SetActive(false);
    }
}