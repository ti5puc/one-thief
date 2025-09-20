using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public enum RotationMode
    {
        Full,
        YOnly,
        XYOnly
    }

    [Header("Settings")]
    [SerializeField] private RotationMode rotationMode = RotationMode.YOnly;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            switch (rotationMode)
            {
                case RotationMode.Full:
                    transform.LookAt(mainCamera.transform);
                    break;
                case RotationMode.YOnly:
                    {
                        Vector3 cameraPos = mainCamera.transform.position;
                        Vector3 objectPos = transform.position;
                        Vector3 flatToCamera = new Vector3(cameraPos.x - objectPos.x, 0f, cameraPos.z - objectPos.z);
                        if (flatToCamera.sqrMagnitude > 0.0001f)
                        {
                            Quaternion yRot = Quaternion.LookRotation(flatToCamera, Vector3.up);
                            transform.rotation = yRot;
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
                            Vector3 euler = lookRot.eulerAngles;
                            euler.z = transform.rotation.eulerAngles.z;
                            transform.rotation = Quaternion.Euler(euler);
                        }
                        break;
                    }
            }
        }
    }
}
