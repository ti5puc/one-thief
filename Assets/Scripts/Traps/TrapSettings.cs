using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(TrapSettings), menuName = "Traps/" + nameof(TrapSettings))]
public class TrapSettings : ScriptableObject
{
    public enum Surface { Ground, Wall, Ceiling }

    [SerializeField] private Surface trapSurface;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapPreview;
    [SerializeField, ShowAssetPreview(128)] private GameObject trapObject;

    public Surface TrapSurface => trapSurface;
    public GameObject TrapPreview => trapPreview;
    public GameObject TrapObject => trapObject;
}
