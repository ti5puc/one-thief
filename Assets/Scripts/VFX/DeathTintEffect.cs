using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class DeathTintEffect : MonoBehaviour
{
    [SerializeField] private float tintMaxIntensity = 0.55f;
    [SerializeField] private float tintFadeInDuration = 0.08f;
    [SerializeField] private float tintHoldDuration = 0.25f;
    [SerializeField] private float tintFadeOutDuration = 1.2f;
    [SerializeField] private float tintLingerIntensity = 0.15f;

    private Vignette vignette;
    private Color originalColor;
    private float originalIntensity;
    private Coroutine tintCoroutine;

    private void Awake()
    {
        var volume = GetComponent<Volume>();

        // Clone the shared profile so we never modify the asset on disk
        volume.profile = Instantiate(volume.sharedProfile);

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogWarning("DeathTintEffect: no Vignette override found on the Volume Profile.");
            return;
        }

        originalColor = vignette.color.value;
        originalIntensity = vignette.intensity.value;

        PlayerDeathIdentifier.OnPlayerDied += Trigger;
        PlayerSave.OnLevelLoaded += ResetEffect;
    }

    private void OnDestroy()
    {
        if (tintCoroutine != null)
            StopCoroutine(tintCoroutine);

        PlayerDeathIdentifier.OnPlayerDied -= Trigger;
        PlayerSave.OnLevelLoaded -= ResetEffect;
    }

    private void Trigger()
    {
        if (vignette == null)
            return;
        if (tintCoroutine != null)
            StopCoroutine(tintCoroutine);

        tintCoroutine = StartCoroutine(TintRoutine());
    }

    private void ResetEffect()
    {
        if (vignette == null)
            return;
        if (tintCoroutine != null)
        {
            StopCoroutine(tintCoroutine);
            tintCoroutine = null;
        }

        vignette.color.Override(originalColor);
        vignette.intensity.Override(originalIntensity);
    }
    
    private IEnumerator TintRoutine()
    {
        vignette.color.Override(Color.red);

        float t = 0f;
        while (t < tintFadeInDuration)
        {
            t += Time.deltaTime;
            vignette.intensity.Override(Mathf.Lerp(originalIntensity, tintMaxIntensity, t / tintFadeInDuration));
            yield return null;
        }
        vignette.intensity.Override(tintMaxIntensity);

        yield return new WaitForSeconds(tintHoldDuration);

        t = 0f;
        while (t < tintFadeOutDuration)
        {
            t += Time.deltaTime;
            vignette.intensity.Override(Mathf.Lerp(tintMaxIntensity, tintLingerIntensity, t / tintFadeOutDuration));
            yield return null;
        }

        // stays at linger intensity + red tint until scene reloads
    }
}
