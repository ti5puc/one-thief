using UnityEngine;
using UnityEngine.Rendering;

namespace ScreenSpaceBoolean
{
    [ExecuteAlways]
    public class AutoLoadPipelineAsset : MonoBehaviour
    {
        public RenderPipelineAsset pipelineAsset;

        private void OnEnable()
        {
            UpdatePipeline();
        }

        void UpdatePipeline()
        {
            //if (pipelineAsset)
            //{
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
            //}
        }
    }
}
