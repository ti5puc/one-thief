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
    private Quaternion initialRotation;
    private Vector3 initialEuler;
    private float initialForwardDistance = 1f;

    private void Start()
    {
        mainCamera = Camera.main;
        initialRotation = transform.rotation;
        initialEuler = transform.eulerAngles;

        // cache initial horizontal (XZ) distance to camera in world space as a fixed baseline
        if (mainCamera != null)
        {
            Vector3 camPos = mainCamera.transform.position;
            Vector3 objPos = transform.position;
            Vector2 camXZ = new Vector2(camPos.x, camPos.z);
            Vector2 objXZ = new Vector2(objPos.x, objPos.z);
            initialForwardDistance = Mathf.Max(0.0001f, Vector2.Distance(camXZ, objXZ));
        }
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            switch (rotationMode)
            {
                case RotationMode.Full:
                    transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - transform.position, Vector3.up) * initialRotation;
                    break;
                case RotationMode.XOnly:
                    {
                        Vector3 toCamera = mainCamera.transform.position - transform.position;
                        if (toCamera.sqrMagnitude > 0.000001f)
                        {
                            // keep initial Y and Z; compute pitch only from world vertical offset (ignore X/Z by using fixed baseline)
                            float dy = toCamera.y; // world-space vertical difference
                            float pitch = Mathf.Rad2Deg * Mathf.Atan2(dy, initialForwardDistance);

                            Vector3 initialRight = initialRotation * -Vector3.right;
                            Vector3 camOffsetXZ = new Vector3(toCamera.x, 0f, toCamera.z);
                            float sideDot = Vector3.Dot(initialRight, camOffsetXZ);

                            if (sideDot < 0f)
                                pitch = -pitch;

                            transform.rotation = Quaternion.Euler(initialEuler.x + pitch, initialEuler.y, initialEuler.z);
                        }
                        break;
                    }
                case RotationMode.YOnly:
                    {
                        Vector3 cameraPos = mainCamera.transform.position;
                        Vector3 objectPos = transform.position;
                        Vector3 flatToCamera = new Vector3(cameraPos.x - objectPos.x, 0f, cameraPos.z - objectPos.z);
                        if (flatToCamera.sqrMagnitude > 0.0001f)
                        {
                            Quaternion yRot = Quaternion.LookRotation(flatToCamera, Vector3.up);
                            transform.rotation = yRot * initialRotation;
                        }
                        break;
                    }
                case RotationMode.XYOnly:
                    {
                        Vector3 cameraPos = mainCamera.transform.position;
                        Vector3 objectPos = transform.position;
                        Vector3 toCamera = cameraPos - objectPos;
                        if (toCamera.sqrMagnitude > 0.0001f)
                        {
                            Quaternion lookRot = Quaternion.LookRotation(toCamera, Vector3.up);
                            Vector3 euler = (lookRot * initialRotation).eulerAngles;
                            euler.z = transform.rotation.eulerAngles.z;
                            transform.rotation = Quaternion.Euler(euler);
                        }
                        break;
                    }
            }
        }
    }
}
