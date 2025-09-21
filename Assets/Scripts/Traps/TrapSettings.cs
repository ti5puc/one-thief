using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(TrapSettings), menuName = "Traps/" + nameof(TrapSettings))]
public class TrapSettings : ScriptableObject
{
    [SerializeField] private string trapName;
    [SerializeField] private LayerMask trapSurface;

    [Space(10)]
    [SerializeField, ShowAssetPreview(128)] private GameObject trapPreview;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapObject;

    public string TrapName => trapName;
    public LayerMask TrapSurface => trapSurface;
    public GameObject TrapPreview => trapPreview;
    public GameObject TrapObject => trapObject;
}
