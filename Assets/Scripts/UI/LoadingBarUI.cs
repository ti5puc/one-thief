using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LoadingBarUI : MonoBehaviour
{
    public static event Action OnLoadingCompleteEvent;

    [Header("UI References")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("Settings")]
    [SerializeField] private float minimumLoadingTime = 2f;
    [SerializeField] private float fillSpeed = 1f;
    [SerializeField] private int numberOfStutters = 3;
    [SerializeField] private float stutterMinDuration = 0.1f;
    [SerializeField] private float stutterMaxDuration = 0.5f;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool gameManagerInitialized;
    [SerializeField, ReadOnly] private bool firebaseInitialized;
    [SerializeField, ReadOnly] private float currentTime;
    [SerializeField, ReadOnly] private float targetFillAmount;
    [SerializeField, ReadOnly] private bool isStuttering;
    [SerializeField, ReadOnly] private int stuttersRemaining;
    
    private bool systemsReady;
    private bool minimumTimePassed;
    private bool loadingComplete;
    private float stutterTimer;
    private float currentFillSpeed;
    private float[] stutterPercentages;
    private int currentStutterIndex;

    private void Start()
    {
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        currentTime = 0f;
        targetFillAmount = 0f;
        systemsReady = false;
        minimumTimePassed = false;
        loadingComplete = false;
        isStuttering = false;
        stutterTimer = 0f;
        currentFillSpeed = fillSpeed;
        currentStutterIndex = 0;
        stuttersRemaining = numberOfStutters;
        
        GenerateStutterPercentages();
        
        // Subscribe to events
        GameManager.OnInitialized += OnGameManagerInitialized;
        FirebaseManager.OnAuthenticationComplete += OnFirebaseInitialized;
        
        // Check if systems are already initialized (important for scene reloads)
        if (GameManager.IsInitialized)
            OnGameManagerInitialized();
        
        if (GameManager.FirebaseManager != null && GameManager.FirebaseManager.IsInitialized)
            OnFirebaseInitialized();
        
        Debug.Log($"[LoadingBarUI] Start complete - SystemsReady: {systemsReady}, GM: {gameManagerInitialized}, FB: {firebaseInitialized}");
    }
    
    private void GenerateStutterPercentages()
    {
        stutterPercentages = new float[numberOfStutters];
        for (int i = 0; i < numberOfStutters; i++)
        {
            stutterPercentages[i] = Random.Range(0.1f, 0.9f);
        }
        Array.Sort(stutterPercentages);
    }

    private void OnDestroy()
    {
        GameManager.OnInitialized -= OnGameManagerInitialized;
        FirebaseManager.OnAuthenticationComplete -= OnFirebaseInitialized;
    }

    private void Update()
    {
        if (loadingComplete)
            return;
        
        currentTime += Time.deltaTime;
        
        if (currentTime >= minimumLoadingTime)
        {
            minimumTimePassed = true;
        }
        
        HandleStuttering();
        
        float timeProgress = Mathf.Clamp01(currentTime / minimumLoadingTime);
        
        if (systemsReady && minimumTimePassed)
        {
            targetFillAmount = 1f;
        }
        else if (!minimumTimePassed)
        {
            targetFillAmount = Mathf.Lerp(0f, 0.9f, timeProgress);
        }
        else
        {
            targetFillAmount = 0.9f;
        }
        
        if (loadingSlider == null)
        {
            Debug.LogError("[LoadingBarUI] Loading slider is null! Cannot update loading bar.");
            return;
        }
        
        loadingSlider.value = Mathf.MoveTowards(loadingSlider.value, targetFillAmount, currentFillSpeed * Time.deltaTime);
        
        if (loadingSlider.value >= 0.99f && systemsReady && minimumTimePassed)
        {
            loadingSlider.value = 1f;
            loadingComplete = true;
            Debug.Log($"[LoadingBarUI] Loading complete! Time: {currentTime:F2}s, Systems: {systemsReady}, MinTime: {minimumTimePassed}");
            OnLoadingComplete();
        }
    }

    private void HandleStuttering()
    {
        if (isStuttering)
        {
            stutterTimer -= Time.deltaTime;
            if (stutterTimer <= 0f)
            {
                isStuttering = false;
                currentFillSpeed = fillSpeed;
                currentStutterIndex++;
                stuttersRemaining--;
            }
        }
        else
        {
            if (currentStutterIndex < stutterPercentages.Length && 
                loadingSlider != null && 
                loadingSlider.value >= stutterPercentages[currentStutterIndex])
            {
                isStuttering = true;
                stutterTimer = Random.Range(stutterMinDuration, stutterMaxDuration);
                currentFillSpeed = fillSpeed * Random.Range(0.05f, 0.3f);
            }
        }
    }

    private void OnGameManagerInitialized()
    {
        Debug.Log($"[LoadingBarUI] GameManager initialized at {currentTime:F2}s");
        gameManagerInitialized = true;
        CheckSystemsReady();
    }

    private void OnFirebaseInitialized()
    {
        Debug.Log($"[LoadingBarUI] Firebase initialized at {currentTime:F2}s");
        firebaseInitialized = true;
        CheckSystemsReady();
    }

    private void CheckSystemsReady()
    {
        if (gameManagerInitialized && firebaseInitialized)
        {
            systemsReady = true;
            Debug.Log($"[LoadingBarUI] All systems ready at {currentTime:F2}s (MinTime: {minimumLoadingTime}s, Passed: {minimumTimePassed})");
        }
    }

    private void OnLoadingComplete()
    {
        Debug.Log("[LoadingBarUI] Loading complete!");
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        Debug.Log("[LoadingBarUI] Invoking OnLoadingCompleteEvent");
        OnLoadingCompleteEvent?.Invoke();
    }
}
