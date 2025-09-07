using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool onlyRotateY = true;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            if (onlyRotateY == false)
            {
                transform.LookAt(mainCamera.transform);
            }
            else
            {
                Vector3 cameraPos = mainCamera.transform.position;
                Vector3 objectPos = transform.position;
                Vector3 flatToCamera = new Vector3(cameraPos.x - objectPos.x, 0f, cameraPos.z - objectPos.z);
                if (flatToCamera.sqrMagnitude > 0.0001f)
                {
                    Quaternion yRot = Quaternion.LookRotation(flatToCamera, Vector3.up);
                    transform.rotation = yRot;
                }
            }
        }
    }
}
