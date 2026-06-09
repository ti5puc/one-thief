using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(1)] // runs after CameraWobble so shake offset is added on top
public class CameraShake : MonoBehaviour
{
    [Header("Shake — Light")]
    [SerializeField] private float lightDuration = 0.25f;
    [SerializeField] private float lightMagnitude = 0.06f;
    [SerializeField] private float lightFrequency = 18f;

    [Header("Shake — Medium")]
    [SerializeField] private float mediumDuration = 0.35f;
    [SerializeField] private float mediumMagnitude = 0.14f;
    [SerializeField] private float mediumFrequency = 22f;

    [Header("Shake — Hard")]
    [SerializeField] private float hardDuration = 0.5f;
    [SerializeField] private float hardMagnitude = 0.28f;
    [SerializeField] private float hardFrequency = 26f;

    private float _shakeDuration;
    private float _shakeElapsed;
    private float _shakeMagnitude;
    private float _shakeFrequency;
    private float _shakeSeed;

    private void Awake()
    {
        GameManager.OnCameraShakeLight += ShakeLight;
        GameManager.OnCameraShakeMedium += ShakeMedium;
        GameManager.OnCameraShakeHard += ShakeHard;
    }

    private void OnDestroy()
    {
        GameManager.OnCameraShakeLight -= ShakeLight;
        GameManager.OnCameraShakeMedium -= ShakeMedium;
        GameManager.OnCameraShakeHard -= ShakeHard;
    }

    private void LateUpdate()
    {
        if (_shakeElapsed >= _shakeDuration) return;

        _shakeElapsed += Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);

        float t = _shakeSeed + _shakeElapsed * _shakeFrequency;
        float nx = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;

        transform.localPosition += new Vector3(nx, ny, 0f) * (_shakeMagnitude * progress);
    }

    private void ShakeLight() => Shake(lightDuration, lightMagnitude, lightFrequency);
    private void ShakeMedium() => Shake(mediumDuration, mediumMagnitude, mediumFrequency);
    private void ShakeHard() => Shake(hardDuration, hardMagnitude, hardFrequency);

    private void Shake(float duration, float magnitude, float frequency)
    {
        // Only override if new shake is stronger than the current one
        float remainingMagnitude = _shakeMagnitude * (1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration));
        if (magnitude < remainingMagnitude) return;

        _shakeDuration = duration;
        _shakeElapsed = 0f;
        _shakeMagnitude = magnitude;
        _shakeFrequency = frequency;
        _shakeSeed = Random.value * 100f;
    }
}
