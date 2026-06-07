using NaughtyAttributes;
using UnityEngine;

public class RandomParticleColor : MonoBehaviour
{
    [SerializeField] private Color[] colors;
    [SerializeField, ReadOnly] private ParticleSystem particleSystem;

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();

        if (colors == null || colors.Length == 0)
            return;

        Color picked = colors[Random.Range(0, colors.Length)];

        var main = particleSystem.main;
        main.startColor = picked;
    }
}
