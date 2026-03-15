using UnityEngine;
using DG.Tweening;

public class TurretBullet : MonoBehaviour
{
    [SerializeField] private TriggerEventSender triggerEventSender;
    [SerializeField] private float spawnDuration = .5f;
    [SerializeField] private float despawnDuration = .5f;

    private Vector3 originalScale;
    private Vector3 velocity;
    private float gravity;
    private bool isActive;

    private void Awake()
    {
        triggerEventSender.OnEnter += HandleTriggerEnter;

        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (!isActive) return;

        // Apply gravity to velocity
        velocity += Vector3.down * gravity * Time.deltaTime;

        // Move bullet
        transform.position += velocity * Time.deltaTime;
    }

    private void OnDestroy()
    {
        triggerEventSender.OnEnter -= HandleTriggerEnter;
    }

    public void Setup(Vector3 initialVelocity, float gravityValue)
    {
        velocity = initialVelocity;
        gravity = gravityValue;
        isActive = true;
        SpawnAnimation();
    }

    private void SpawnAnimation()
    {
        transform.DOScale(originalScale, spawnDuration).SetEase(Ease.OutBack);
    }

    private void HandleTriggerEnter(Collider other)
    {
        if (other.CompareTag("Turret"))
            return;

        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponent<PlayerDeathIdentifier>();
            if (controller != null)
                controller.Death();
        }

        isActive = false;
        triggerEventSender.gameObject.SetActive(false);
        DespawnAnimation();
    }

    private void DespawnAnimation()
    {
        transform.DOScale(Vector3.zero, despawnDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
