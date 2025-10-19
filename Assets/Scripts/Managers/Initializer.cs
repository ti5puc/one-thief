using UnityEngine;

[DefaultExecutionOrder(-999)]
public class Initializer : MonoBehaviour
{
    [SerializeField] private GameObject playerOnScene;

    [Space(10)]
    [SerializeField] private GameObject layoutOnScene;
    [SerializeField] private GameObject[] layouts;

    private void Awake()
    {
        if (playerOnScene != null)
            gameObject.SetActive(false);

        if (layoutOnScene != null && layouts.Length > 0)
        {
            Destroy(layoutOnScene);

            int randomIndex = Random.Range(0, layouts.Length);
            layoutOnScene = Instantiate(layouts[randomIndex]);
        }

        if (playerOnScene != null)
            gameObject.SetActive(true);
    }
}
