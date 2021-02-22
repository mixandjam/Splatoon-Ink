using UnityEngine;
using UnityEngine.Rendering;

public class Paintable : MonoBehaviour {
    const int TEXTURE_SIZE = 1024;

    RenderTexture extendIslandsRenderTexture;
    RenderTexture maskRenderTexture;
    RenderTexture supportTexture;
    
    Renderer rend;

    int maskTextureID = Shader.PropertyToID("_MaskTexture");

    public RenderTexture getMask() => maskRenderTexture;
    public RenderTexture getExtend() => extendIslandsRenderTexture;
    public RenderTexture getSupport() => supportTexture;
    public Renderer getRenderer() => rend;

    void Start() {
        maskRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);

        extendIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        extendIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        supportTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        supportTexture.filterMode =  FilterMode.Bilinear;

        rend = GetComponent<Renderer>();
        rend.material.SetTexture(maskTextureID, extendIslandsRenderTexture);

        CommandBuffer command = new CommandBuffer();
        command.name = "CommandBuffer - " + gameObject.name;
        command.SetRenderTarget(maskRenderTexture);
        command.SetRenderTarget(extendIslandsRenderTexture);
        command.SetRenderTarget(supportTexture);
        Graphics.ExecuteCommandBuffer(command);
    }

    void OnDisable(){
        maskRenderTexture.Release();
        extendIslandsRenderTexture.Release();
        supportTexture.Release();
    }
}