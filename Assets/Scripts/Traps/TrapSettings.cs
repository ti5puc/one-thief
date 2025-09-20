using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(TrapSettings), menuName = "Traps/" + nameof(TrapSettings))]
public class TrapSettings : ScriptableObject
{
    [SerializeField] private LayerMask trapSurface;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapPreview;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapObject;

    public LayerMask TrapSurface => trapSurface;
    public GameObject TrapPreview => trapPreview;
    public GameObject TrapObject => trapObject;
}
