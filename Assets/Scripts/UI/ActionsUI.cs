using UnityEngine;
using UnityEngine.UI;

public class ActionsUI : MonoBehaviour
{
    [Header("Referências")]
    public Player player;
    public Transform jumpsParent;
    public GameObject jumpIconPrefab;

    [Header("Ícone de Andar")]
    public GameObject shiftIcon;

    [Header("Configuração")]
    private Image[] icons;

    void OnEnable()
    {
        if (player != null)
            player.OnMoveChanged += HandleMoveChanged;
    }

    void OnDisable()
    {
        if (player != null)
            player.OnMoveChanged -= HandleMoveChanged;
    }

    void Start()
    {
        BuildUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void BuildUI()
    {
        // limpa ícones antigos
        foreach (Transform child in jumpsParent)
            Destroy(child.gameObject);

        // cria novos ícones baseados no maxAirJumps
        icons = new Image[player.maxAirJumps];

        for (int i = 0; i < player.maxAirJumps; i++)
        {
            GameObject iconObj = Instantiate(jumpIconPrefab, jumpsParent);
            icons[i] = iconObj.GetComponent<Image>();
        }
    }

    void UpdateUI()
    {
        // quantidade que o player ainda tem no ar
        int remaining = player.airJumpsRemaining;

        for (int i = 0; i < icons.Length; i++)
        {
            if (i < remaining)
            {
                // pulo disponível em cor
                icons[i].color = new Color(1, 1, 1, 1f);
            }
            else
            {
                // pulo usado transparente
                icons[i].color = new Color(1, 1, 1, 0.2f);
            }
        }
    }

    void HandleMoveChanged(bool isMoving, bool isSprinting)
    {
        if (shiftIcon == null)
            return;

        shiftIcon.SetActive(!isSprinting);
    }
}
