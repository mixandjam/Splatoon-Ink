#ifndef METABALL_INCLUDE
#define METABALL_INCLUDE
// Credit to Scratchapixel at
// https://www.scratchapixel.com/lessons/advanced-rendering/rendering-distance-fields/basic-sphere-tracer
// for the explanation on Metaballs and example code

#define MAX_PARTICLES 256

float4 _ParticlesPos[MAX_PARTICLES];
float _ParticlesSize[MAX_PARTICLES];
float _NumParticles;

float GetDistanceMetaball(float3 from, float densityThreshold)
{
    float sumDensity = 0;
    float sumRi = 0;
    float minDistance = 100000;
    for (int i = 0; i < _NumParticles; ++i)
    {
        float4 center = _ParticlesPos[i];
        float radius = 0.5 * _ParticlesSize[i];
        float r = length(center - from);
        if (r <= radius)
        {
            sumDensity += 2 * (r * r * r) / (radius * radius * radius) - 3 * (r * r) / (radius * radius) + 1;
        }
        minDistance = min(minDistance, r - radius);
        sumRi += radius;
    }

    return max(minDistance, (densityThreshold - sumDensity) / (3 / 2.0 * sumRi));
}

float3 CalculateNormalMetaball(float3 from, float densityThreshold)
{
    float delta = 10e-5;
    float3 normal = float3(
        GetDistanceMetaball(from + float3(delta, 0, 0), densityThreshold) - GetDistanceMetaball(from + float3(-delta, 0, 0), densityThreshold),
        GetDistanceMetaball(from + float3(0, delta, 0), densityThreshold) - GetDistanceMetaball(from + float3(-0, -delta, 0), densityThreshold),
        GetDistanceMetaball(from + float3(0, 0, delta), densityThreshold) - GetDistanceMetaball(from + float3(0, 0, -delta), densityThreshold)
    );
    return normalize(normal);
}


void SphereTraceMetaballs_float(float3 WorldPosition, float DensityThreshold, out float3 PositionWS, out float3 NormalWS, out float Alpha,
    out float3 ViewDirection)
{
    #if defined(SHADERGRAPH_PREVIEW)
    PositionWS = float3(0, 0, 0);
    NormalWS = float3(0, 0, 0);
    ViewDirection = float3(0, 0, 0);
    Alpha = 0;
    #else
    float maxDistance = 100;
    float threshold = 0.00001;
    float t = 0;
    int numSteps = 0;

    float outAlpha = 0;

    float3 viewPosition = GetCurrentViewPosition();
    half3 viewDir = SafeNormalize(WorldPosition - viewPosition);
    while (t < maxDistance)
    {
        float minDistance = 1000000;
        float3 from = viewPosition + t * viewDir;
        float d = GetDistanceMetaball(from, DensityThreshold);
        if (d < minDistance)
        {
            minDistance = d;
        }

        if (minDistance <= threshold * t)
        {
            PositionWS = from;
            NormalWS = CalculateNormalMetaball(from, DensityThreshold);
            ViewDirection = viewDir;
            outAlpha = 1;
            break;
        }

        t += minDistance;
        ++numSteps;
    }
    
    Alpha = outAlpha;
    #endif
}

void SphereTraceMetaballs_half(half3 WorldPosition, float DensityThreshold, out half3 PositionWS, out half3 NormalWS,
    out half Alpha, out half3 ViewDirection)
{
    #if defined(SHADERGRAPH_PREVIEW)
    PositionWS = half3(0, 0, 0);
    NormalWS = half3(0, 0, 0);
    ViewDirection = half3(0, 0, 0);
    Alpha = 0;
    #else
    half maxDistance = 100;
    half threshold = 0.00001;
    half t = 0;
    int numSteps = 0;

    half outAlpha = 0;
    
    half3 viewPosition = GetCurrentViewPosition();
    half3 viewDir = SafeNormalize(WorldPosition - viewPosition);
    while (t < maxDistance)
    {
        half minDistance = 1000000;
        half3 from = viewPosition + t * viewDir;
        half d = GetDistanceMetaball(from, DensityThreshold);
        if (d < minDistance)
        {
            minDistance = d;
        }

        if (minDistance <= threshold * t)
        {
            PositionWS = from;
            NormalWS = CalculateNormalMetaball(from, DensityThreshold);
            ViewDirection = viewDir;
            outAlpha = 1;
            break;
        }

        t += minDistance;
        ++numSteps;
    }
    
    Alpha = outAlpha;
    #endif
}

void PBR_float(float3 positionWS, half3 normalWS, half3 viewDirectionWS, half3 bakedGI, half3 albedo,
    half metallic, half3 specular, half smoothness, half occlusion, half3 emission, half alpha, out float3 Color)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Color = float3(1, 1, 1);
    #else
    InputData inputData;
    inputData.positionWS = positionWS;
    inputData.normalWS = NormalizeNormalPerPixel(normalWS);
    inputData.viewDirectionWS = SafeNormalize(-viewDirectionWS);
    inputData.shadowCoord = half4(0, 0, 0, 0);
    inputData.fogCoord = 0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = half2(0, 0);
    inputData.shadowMask = half4(0, 0, 0, 0);
    inputData.bakedGI = bakedGI;
    Color = UniversalFragmentPBR(inputData, albedo, metallic, specular, smoothness, occlusion, emission, alpha);
    #endif
}

void PBR_half(half3 positionWS, half3 normalWS, half3 viewDirectionWS, half3 bakedGI, half3 albedo,
    half metallic, half3 specular, half smoothness, half occlusion, half3 emission, half alpha, out half3 Color)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Color = half3(1, 1, 1);
    #else
    InputData inputData;
    inputData.positionWS = positionWS;
    inputData.normalWS = NormalizeNormalPerPixel(normalWS);
    inputData.viewDirectionWS = SafeNormalize(-viewDirectionWS);
    inputData.shadowCoord = half4(0, 0, 0, 0);
    inputData.fogCoord = 0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = half2(0, 0);
    inputData.shadowMask = half4(0, 0, 0, 0);
    inputData.bakedGI = bakedGI;
    Color = UniversalFragmentPBR(inputData, albedo, metallic, specular, smoothness, occlusion, emission, alpha);
    #endif
}

#endif
