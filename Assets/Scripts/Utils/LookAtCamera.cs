using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public enum RotationMode
    {
        Full,
        XOnly,
        YOnly,
        XYOnly
    }

    [Header("Settings")]
    [SerializeField] private RotationMode rotationMode = RotationMode.YOnly;

    private Camera mainCamera;
    private Quaternion initialLocalRotation;

    private void Start()
    {
        mainCamera = Camera.main;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (mainCamera == null) return;

        // Inverse of parent world rotation brings world-space vectors into parent-local space.
        // transform.parent.rotation is always the full world rotation of the parent chain.
        Quaternion invParentRot = transform.parent != null
            ? Quaternion.Inverse(transform.parent.rotation)
            : Quaternion.identity;

        Vector3 toCameraLocal = invParentRot * (mainCamera.transform.position - transform.position);
        Vector3 initEuler = initialLocalRotation.eulerAngles;

        switch (rotationMode)
        {
            case RotationMode.Full:
                {
                    if (toCameraLocal.sqrMagnitude > 0.0001f)
                    {
                        transform.localRotation = Quaternion.LookRotation(toCameraLocal, Vector3.up) * initialLocalRotation;
                    }
                    break;
                }
            case RotationMode.XOnly:
                {
                    if (toCameraLocal.sqrMagnitude > 0.000001f)
                    {
                        float dxz = Mathf.Max(new Vector2(toCameraLocal.x, toCameraLocal.z).magnitude, 0.0001f);
                        float pitch = Mathf.Atan2(toCameraLocal.y, dxz) * Mathf.Rad2Deg;
                        transform.localRotation = Quaternion.Euler(initEuler.x + pitch, initEuler.y, initEuler.z);
                    }
                    break;
                }
            case RotationMode.YOnly:
                {
                    Vector3 flatLocal = new Vector3(toCameraLocal.x, 0f, toCameraLocal.z);
                    if (flatLocal.sqrMagnitude > 0.0001f)
                    {
                        float yAngle = Quaternion.LookRotation(flatLocal, Vector3.up).eulerAngles.y + 180f;
                        transform.localRotation = Quaternion.Euler(initEuler.x, yAngle, initEuler.z);
                    }
                    break;
                }
            case RotationMode.XYOnly:
                {
                    if (toCameraLocal.sqrMagnitude > 0.0001f)
                    {
                        Vector3 lookEuler = Quaternion.LookRotation(toCameraLocal, Vector3.up).eulerAngles;
                        transform.localRotation = Quaternion.Euler(lookEuler.x, lookEuler.y, initEuler.z);
                    }
                    break;
                }
        }
    }
}
