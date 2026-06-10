using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeadMessageUI : MonoBehaviour
{
    [SerializeField] private float delayToShow = 1f;
    [SerializeField] private TMP_Text deadMessageText;
    [SerializeField] private List<string> messages = new()
    {
        "Morreste!",
        "Que pena!",
        "Não foi dessa vez!",
        "Tentativa fracassada!",
        "A armadilha venceu!",
        "Fim de linha!",
        "Quase lá!",
    };

    private const string Suffix = "<br>Aperte 'ESC' para resetar";

    private Coroutine showCoroutine;

    private void Awake()
    {
        PlayerDeathIdentifier.OnPlayerDied += ShowAfterDelay;
        PlayerSave.OnLevelLoaded += ResetMessage;
        GameManager.OnGameStateChanged += ResetMessage;

        Hide();
    }

    private void OnDestroy()
    {
        PlayerDeathIdentifier.OnPlayerDied -= ShowAfterDelay;
        PlayerSave.OnLevelLoaded -= ResetMessage;
        GameManager.OnGameStateChanged -= ResetMessage;
    }

    private void ShowAfterDelay()
    {
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        deadMessageText.text = string.Empty;

        yield return new WaitForSeconds(delayToShow);

        if (messages.Count > 0)
            deadMessageText.text = messages[Random.Range(0, messages.Count)] + Suffix;

        deadMessageText.gameObject.SetActive(true);
        showCoroutine = null;
    }

    private void ResetMessage(GameState _) => Hide();
    private void ResetMessage() => Hide();

    private void Hide()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        deadMessageText.gameObject.SetActive(false);
    }
}
