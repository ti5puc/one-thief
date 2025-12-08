using UnityEngine;
using UnityEngine.UI;

public class ActionsUI : MonoBehaviour
{
    [Header("Refer�ncias")]
    public Player player;
    public Transform jumpsParent;
    public GameObject jumpIconPrefab;

    [Header("�cone de Andar")]
    public GameObject shiftIcon;

    [Header("Configura��o")]
    private Image[] icons;
    
    private Color defaultColor = Color.white;

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

        // cria novos ícones baseados no maxAirJumps + 1 (inclui pulo do chão)
        icons = new Image[player.maxAirJumps + 1];

        for (int i = 0; i < icons.Length; i++)
        {
            GameObject iconObj = Instantiate(jumpIconPrefab, jumpsParent);
            icons[i] = iconObj.GetComponent<Image>();
        }
        
        defaultColor = icons[0].color;
    }

    void UpdateUI()
    {
        if (player.isGrounded)
        {
            // Se está no chão, todos os ícones ficam coloridos
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].color = defaultColor;
            }
        }
        else
        {
            // Se está no ar, primeiro ícone (pulo do chão) fica transparente
            icons[0].color = new Color(1, 1, 1, 0.2f);
            // Os próximos ícones refletem os pulos aéreos usados e restantes
            for (int i = 1; i < icons.Length; i++)
            {
                if (i <= player.maxAirJumps - player.airJumpsRemaining)
                    icons[i].color = new Color(1, 1, 1, 0.2f); // usado
                else
                    icons[i].color = defaultColor; // disponível
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