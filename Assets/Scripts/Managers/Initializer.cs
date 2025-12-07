using System;
using UnityEngine;

[DefaultExecutionOrder(-999)]
public class Initializer : MonoBehaviour
{
    [SerializeField] private GameObject playerOnScene;

    [Space(10)]
    [SerializeField] private GameObject layoutOnScene;
    [SerializeField] private GameObject[] layouts;

    private bool hasInitialized;
    
    private void Awake()
    {
        PlayerSave.OnLevelLoaded += ActivateObjects;
    }

    private void OnDestroy()
    {
        PlayerSave.OnLevelLoaded -= ActivateObjects;
    }

    private void Start()
    {
        if (playerOnScene != null)
            gameObject.SetActive(false);

        if (layoutOnScene != null && layouts.Length > 0)
        {
            Destroy(layoutOnScene);

            int layoutIndex = GameManager.NextLayoutIndex;
            layoutOnScene = Instantiate(layouts[layoutIndex]);
        }

        if (playerOnScene != null)
            gameObject.SetActive(true);
        
        hasInitialized = true;
    }

    private void ActivateObjects()
    {
        if (hasInitialized == false)
            return;
        
        foreach (Transform child in layoutOnScene.transform)
        {
            Debug.Log($"[Initializer] Ativando objeto filho: {child.gameObject.name}");
            
            child.gameObject.SetActive(true);
            
            if (child.TryGetComponent(out Ground ground))
                ground.ResetTransform();
        }
    }
}
