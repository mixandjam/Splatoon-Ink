void GetDepth_float(float4 ObjectPosition, out float Depth)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Depth = 0.5;
    #else
    Depth = TransformObjectToHClip(ObjectPosition).w * _ProjectionParams.w;
    #endif
}

void GetDepth_half(half4 ObjectPosition, out half Depth)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Depth = 0.5h;
    #else
    Depth = TransformObjectToHClip(ObjectPosition).w * _ProjectionParams.w;
    #endif
}