using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Hata giderildiği için pragma uyarısını devre dışı bırakmaya gerek kalmadı.
// #pragma warning disable CS0618 <- BU SATIR KALDIRILDI

namespace FlatKit {
public class FlatKitOutline : ScriptableRendererFeature {
    class OutlinePass : ScriptableRenderPass {
        private RTHandle _destination;
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Outline");
        private readonly Material _outlineMaterial;
        
        // DEĞİŞİKLİK: RenderTargetHandle, geçici RT'nin ID'sini tutan bir int ile değiştirildi.
        private int _temporaryColorTextureID;
        
        private bool _usingTemporaryRT;

        public OutlinePass(Material outlineMaterial) {
            _outlineMaterial = outlineMaterial;
            // DEĞİŞİKLİK: Geçici Render Texture için bir ID oluşturuldu.
            _temporaryColorTextureID = Shader.PropertyToID("_TemporaryColorTexture");
        }

        public void Setup(OutlineSettings settings, RTHandle destination = null) {
            _destination = destination;
#if UNITY_2020_3_OR_NEWER
            ConfigureInput(ScriptableRenderPassInput.Color |
                           (settings.useDepth ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None) |
                           (settings.useNormals ? ScriptableRenderPassInput.Normal : ScriptableRenderPassInput.None));
#endif
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler)) {
                RenderTextureDescriptor opaqueDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDescriptor.depthBufferBits = 0;

#if UNITY_2022_1_OR_NEWER
                var cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
                var cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTarget;
#endif

                if (_destination == null) {
                    _usingTemporaryRT = true;
                    // DEĞİŞİKLİK: .id yerine doğrudan ID değişkeni kullanıldı.
                    cmd.GetTemporaryRT(_temporaryColorTextureID, opaqueDescriptor, FilterMode.Point);
                    // DEĞİŞİKLİK: .Identifier() yerine doğrudan ID değişkeni kullanıldı.
                    cmd.Blit(cameraTargetHandle, _temporaryColorTextureID, _outlineMaterial, 0);
                    cmd.Blit(_temporaryColorTextureID, cameraTargetHandle);
                } else {
                    cmd.Blit(cameraTargetHandle, _destination, _outlineMaterial, 0);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#if UNITY_2020_3_OR_NEWER
        public override void OnCameraCleanup(CommandBuffer cmd) {
#else
        public override void FrameCleanup(CommandBuffer cmd) {
#endif
            if (_usingTemporaryRT) {
                // DEĞİŞİKLİK: .id yerine doğrudan ID değişkeni kullanıldı.
                cmd.ReleaseTemporaryRT(_temporaryColorTextureID);
            }
        }
    }

    [Tooltip("To create new settings use 'Create > FlatKit > Outline Settings'.")]
    public OutlineSettings settings;

    private Material _material;
    private OutlinePass _outlinePass;
    
    // DEĞİŞİKLİK: Hataya sebep olan ve kullanılmayan eski RenderTargetHandle satırı kaldırıldı.
    // private RenderTargetHandle _outlineTexture; 

    private static readonly string ShaderName = "Hidden/FlatKit/OutlineFilter";
    private static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
    private static readonly int Thickness = Shader.PropertyToID("_Thickness");
    private static readonly int DepthThresholdMin = Shader.PropertyToID("_DepthThresholdMin");
    private static readonly int DepthThresholdMax = Shader.PropertyToID("_DepthThresholdMax");
    private static readonly int NormalThresholdMin = Shader.PropertyToID("_NormalThresholdMin");
    private static readonly int NormalThresholdMax = Shader.PropertyToID("_NormalThresholdMax");
    private static readonly int ColorThresholdMin = Shader.PropertyToID("_ColorThresholdMin");
    private static readonly int ColorThresholdMax = Shader.PropertyToID("_ColorThresholdMax");

    public override void Create() {
        if (settings == null) {
            Debug.LogWarning("[FlatKit] Missing Outline Settings");
            return;
        }

#if UNITY_EDITOR
        ShaderIncludeUtilities.AddAlwaysIncludedShader(ShaderName);
#endif

        InitMaterial();

        _outlinePass = new OutlinePass(_material) {
            renderPassEvent = settings.renderEvent
        };
        
        // DEĞİŞİKLİK: Hataya sebep olan .Init() satırı kaldırıldı.
        // _outlineTexture.Init("_OutlineTexture");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
#if UNITY_EDITOR
        if (renderingData.cameraData.isPreviewCamera) {
            return;
        }
#endif

        if (settings == null) {
            Debug.LogWarning("[FlatKit] Missing Outline Settings");
            return;
        }

        InitMaterial();

        _outlinePass.Setup(settings);
        renderer.EnqueuePass(_outlinePass);
    }

    private void InitMaterial() {
        if (_material == null) {
            var shader = Shader.Find(ShaderName);
            if (shader == null) {
                return;
            }

            _material = new Material(shader);
        }

        if (_material == null) {
            Debug.LogWarning("[FlatKit] Missing Outline Material");
        }

        UpdateShader();
    }

    private void UpdateShader() {
        if (_material == null) {
            return;
        }

        const string depthKeyword = "OUTLINE_USE_DEPTH";
        if (settings.useDepth) {
            _material.EnableKeyword(depthKeyword);
        } else {
            _material.DisableKeyword(depthKeyword);
        }

        const string normalsKeyword = "OUTLINE_USE_NORMALS";
        if (settings.useNormals) {
            _material.EnableKeyword(normalsKeyword);
        } else {
            _material.DisableKeyword(normalsKeyword);
        }

        const string colorKeyword = "OUTLINE_USE_COLOR";
        if (settings.useColor) {
            _material.EnableKeyword(colorKeyword);
        } else {
            _material.DisableKeyword(colorKeyword);
        }

        const string outlineOnlyKeyword = "OUTLINE_ONLY";
        if (settings.outlineOnly) {
            _material.EnableKeyword(outlineOnlyKeyword);
        } else {
            _material.DisableKeyword(outlineOnlyKeyword);
        }

        const string resolutionInvariantKeyword = "RESOLUTION_INVARIANT_THICKNESS";
        if (settings.resolutionInvariant) {
            _material.EnableKeyword(resolutionInvariantKeyword);
        } else {
            _material.DisableKeyword(resolutionInvariantKeyword);
        }

        _material.SetColor(EdgeColor, settings.edgeColor);
        _material.SetFloat(Thickness, settings.thickness);

        _material.SetFloat(DepthThresholdMin, settings.minDepthThreshold);
        _material.SetFloat(DepthThresholdMax, settings.maxDepthThreshold);

        _material.SetFloat(NormalThresholdMin, settings.minNormalsThreshold);
        _material.SetFloat(NormalThresholdMax, settings.maxNormalsThreshold);

        _material.SetFloat(ColorThresholdMin, settings.minColorThreshold);
        _material.SetFloat(ColorThresholdMax, settings.maxColorThreshold);
    }
}
}