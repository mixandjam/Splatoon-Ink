using UnityEngine;
using UnityEngine.Rendering;

public class PaintManager : Singleton<PaintManager>{

    public Shader texturePaint;
    public Shader extendIslands;

    int positionID = Shader.PropertyToID("_PainterPosition");
    int hardnessID = Shader.PropertyToID("_Hardness");
    int strengthID = Shader.PropertyToID("_Strength");
    int radiusID = Shader.PropertyToID("_Radius");
    int blendOpID = Shader.PropertyToID("_BlendOp");
    int colorID = Shader.PropertyToID("_PainterColor");
    int textureID = Shader.PropertyToID("_MainTex");

    Material paintMaterial;
    Material extendMaterial;

    CommandBuffer command;

    void Start(){
        paintMaterial = new Material(texturePaint);
        extendMaterial = new Material(extendIslands);
        command = new CommandBuffer();
        command.name = "CommmandBuffer - " + gameObject.name;
    }

    public void paint(Paintable paintable, Vector3 pos, float radius = 1f, float hardness = .5f, float strength = .5f, Color? color = null){
        
        RenderTexture mask = paintable.getMask();
        RenderTexture extend = paintable.getExtend();
        RenderTexture support = paintable.getSupport();
        Renderer rend = paintable.getRenderer();

        paintMaterial.SetVector(positionID, pos);
        paintMaterial.SetFloat(hardnessID, hardness);
        paintMaterial.SetFloat(strengthID, strength);
        paintMaterial.SetFloat(radiusID, radius);
        paintMaterial.SetTexture(textureID, support);
        paintMaterial.SetColor(colorID, color ?? Color.red);

        command.SetRenderTarget(mask);
        command.DrawRenderer(rend, paintMaterial, 0);

        command.SetRenderTarget(support);
        command.Blit(mask, support);

        command.SetRenderTarget(extend);
        command.Blit(mask, extend, extendMaterial);

        Graphics.ExecuteCommandBuffer(command);
        command.Clear();
    }

}
