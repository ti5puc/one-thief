using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace ScreenSpaceBoolean
{

    [System.Serializable]
    public class SubstracteeBackDepthPassSettings
    {
        public bool enabled = false;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
        public Material material;
        public Shader shader;// this is a reference to ensure to get the shader into the build
    }

    [System.Serializable]
    public class SubstractionDepthPassSettings
    {
        public bool enabled = false;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
        public Material substracteeFrontDepthMaterial;
        public Shader substracteeFrontDepthShader;// this is a reference to ensure to get the shader into the build
        public Material depthMaskMaterial;
        public Shader depthMaskShader;// this is a reference to ensure to get the shader into the build
    }


    public class ScreenSpaceBooleanRenderPassFeature : ScriptableRendererFeature
    {

        public SubstracteeBackDepthPassSettings substracteeBackDepthPassSettings;

        public SubstractionDepthPassSettings substractionDepthPassSettings;


        class SubstracteeBackDepthPass : ScriptableRenderPass
        {
            Material depthPassMaterial;
            public SubstracteeBackDepthPass(Material material)
            {
                depthPassMaterial = material;
            }
            // This class stores the data needed by the RenderGraph pass.
            // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
            class PassData
            {
                public Material material;
                public int depthmapID;
            }

            // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
            // It is used to execute draw commands.
            static void ExecutePass(PassData data, RasterGraphContext context)
            {
                context.cmd.ClearRenderTarget(true, true, Color.white, 0f);
                foreach (Subtractee sr in Subtractee.instances)
                {
                    Mesh mesh = sr.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh != null)
                    {
                        for (int i = 0; i < mesh.subMeshCount; i++)
                            context.cmd.DrawRenderer(sr.GetComponent<Renderer>(), data.material, i, 1);
                    }
                }
            }

            // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
            // FrameData is a context container through which URP resources can be accessed and managed.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                const string passName = "SubstracteeBackDepthPassPass";

                // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                {
                    // Use this scope to set the required inputs and outputs of the pass and to
                    // setup the passData with the required properties needed at pass execution time.

                    // Make use of frameData to access resources and camera data through the dedicated containers.
                    // Eg:
                     UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();////
                    var descriptor = cameraData.cameraTargetDescriptor;

                    //descriptor.graphicsFormat = GraphicsFormat.None;
                    descriptor.msaaSamples = 1;
                    TextureHandle dest = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_SubtracteeBackDepth", true);

                    if (!dest.IsValid())
                        return;



                    // Setup pass inputs and outputs through the builder interface.
                    // Eg:
                    // builder.UseTexture(sourceTexture);
                    // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, cameraData.cameraTargetDescriptor, "Destination Texture", false);
                    passData.depthmapID = Shader.PropertyToID("_SubtracteeBackDepth");
                    passData.material = depthPassMaterial;
                    // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);////
                    builder.SetRenderAttachmentDepth(dest, AccessFlags.Write);

                    builder.SetGlobalTextureAfterPass(dest, passData.depthmapID);

                    builder.AllowPassCulling(false);

                    // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }
            }

            // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
            }

            // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
            }

            // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }
        }

        class SubstractionDepthPass : ScriptableRenderPass
        {
            Material substracteeMaterial;
            Material maskMaterial;
            int maskDrawNum;
            public SubstractionDepthPass(Material _substracteeMaterial, Material _maskMaterial, int _maskDrawNum)
            {
                substracteeMaterial = _substracteeMaterial;
                maskMaterial = _maskMaterial;
                maskDrawNum = _maskDrawNum;
            }
            // This class stores the data needed by the RenderGraph pass.
            // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
            class PassData
            {
                public Material substracteeMaterial;
                public Material maskMaterial;
                public int maskDrawNum;
                public int depthmapID;
            }

            // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
            // It is used to execute draw commands.
            static void ExecutePass(PassData data, RasterGraphContext context)
            {
                //context.cmd.ClearRenderTarget(true, true, Color.white, 0f);
                context.cmd.ClearRenderTarget(true, true, Color.black);
                foreach (Subtractee sr in Subtractee.instances)
                {
                    Mesh mesh = sr.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh != null)
                    {
                        for (int i = 0; i < mesh.subMeshCount; i++)
                            context.cmd.DrawRenderer(sr.GetComponent<Renderer>(), data.substracteeMaterial, i, 0);
                    }
                }
                for (int j = 0; j < data.maskDrawNum; j++) 
                { 
                    foreach (Subtractor sr in Subtractor.instances)
                    {
                        Mesh mesh = sr.GetComponent<MeshFilter>()?.sharedMesh;
                        if (mesh != null)
                        {
                            for (int i = 0; i < mesh.subMeshCount; i++)
                                context.cmd.DrawRenderer(sr.GetComponent<Renderer>(), data.maskMaterial, i);
                        }
                    }
                }

            }

            // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
            // FrameData is a context container through which URP resources can be accessed and managed.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                const string passName = "SubstractionDepthPass";

                // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
                {
                    // Use this scope to set the required inputs and outputs of the pass and to
                    // setup the passData with the required properties needed at pass execution time.

                    // Make use of frameData to access resources and camera data through the dedicated containers.
                    // Eg:
                    UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();////
                    var descriptor = cameraData.cameraTargetDescriptor;

                    //descriptor.graphicsFormat = GraphicsFormat.None;
                    descriptor.msaaSamples = 1;
                    TextureHandle dest = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_SubtractionDepth", true);

                    if (!dest.IsValid())
                        return;



                    // Setup pass inputs and outputs through the builder interface.
                    // Eg:
                    // builder.UseTexture(sourceTexture);
                    // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, cameraData.cameraTargetDescriptor, "Destination Texture", false);
                    passData.depthmapID = Shader.PropertyToID("_SubtractionDepth");
                    passData.substracteeMaterial = substracteeMaterial;
                    passData.maskMaterial = maskMaterial;
                    passData.maskDrawNum = maskDrawNum;
                    // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);////
                    builder.SetRenderAttachmentDepth(dest, AccessFlags.Write);

                    builder.SetGlobalTextureAfterPass(dest, passData.depthmapID);

                    builder.AllowPassCulling(false);

                    // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }
            }

            // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
            }

            // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
            }

            // NOTE: This method is part of the compatibility rendering path, please use the Render Graph API above instead.
            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }
        }

        SubstracteeBackDepthPass m_SubstracteeBackDepthPass;
        SubstractionDepthPass m_SubstractionDepthPass;

        /// <inheritdoc/>
        public override void Create()
        {
            if (URPsettings.Instance == null) return;
            m_SubstracteeBackDepthPass = new SubstracteeBackDepthPass(substracteeBackDepthPassSettings.material);

            // Configures where the render pass should be injected.
            m_SubstracteeBackDepthPass.renderPassEvent = substracteeBackDepthPassSettings.renderPassEvent;
            m_SubstractionDepthPass = new SubstractionDepthPass(substractionDepthPassSettings.substracteeFrontDepthMaterial, substractionDepthPassSettings.depthMaskMaterial, URPsettings.Instance.maskDrawNum);
            m_SubstractionDepthPass.renderPassEvent = substractionDepthPassSettings.renderPassEvent;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (URPsettings.Instance == null) return;
            if (substracteeBackDepthPassSettings.enabled)
                renderer.EnqueuePass(m_SubstracteeBackDepthPass);
            if (substractionDepthPassSettings.enabled)
                renderer.EnqueuePass(m_SubstractionDepthPass);
        }
    }
}