using UnityEngine;
namespace ScreenSpaceBoolean
{
    [ExecuteInEditMode]
    public class URPsettings : MonoBehaviour
    {
        public static URPsettings Instance { get; private set; }
        [Range(1, 2)]
        public int maskDrawNum = 1;
        public SubstracteeBackDepthPassSettings SBDPsettings;
        public SubstractionDepthPassSettings SDPsettings;

        //void Awake()
        void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            Instance = this;

        }
    }
}