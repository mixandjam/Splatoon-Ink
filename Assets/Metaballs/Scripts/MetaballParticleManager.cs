using System;
using UnityEngine;

[ExecuteAlways]
public class MetaballParticleManager : MonoBehaviour
{
    [SerializeField] ParticleSystem _particleSystem;
    Renderer _renderer;
    MaterialPropertyBlock _materialPropertyBlock;

    const int MaxParticles = 256;
    int _numParticles;

    ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[MaxParticles];

    readonly Vector4[] _particlesPos = new Vector4[MaxParticles];
    readonly float[] _particlesSize = new float[MaxParticles];
    
    static readonly int NumParticles = Shader.PropertyToID("_NumParticles");
    static readonly int ParticlesSize = Shader.PropertyToID("_ParticlesSize");
    static readonly int ParticlesPos = Shader.PropertyToID("_ParticlesPos");

    void OnEnable()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetInt(NumParticles, 0);
        
        _renderer = _particleSystem.GetComponent<Renderer>();
        _renderer.SetPropertyBlock(_materialPropertyBlock);
    }

    void OnDisable()
    {
        _materialPropertyBlock.Clear();
        _materialPropertyBlock = null;
        _renderer.SetPropertyBlock(null);
    }

    void Update()
    {
        _numParticles = _particleSystem.particleCount;
        _particleSystem.GetParticles(_particles, MaxParticles);

        int i = 0;
        foreach (var particle in _particles)
        {
            _particlesPos[i] = particle.position;
            _particlesSize[i] = particle.GetCurrentSize(_particleSystem);
            ++i;
            
            if (i >= _numParticles) break;
        }
        
        _materialPropertyBlock.SetVectorArray(ParticlesPos, _particlesPos);
        _materialPropertyBlock.SetFloatArray(ParticlesSize, _particlesSize);
        _materialPropertyBlock.SetInt(NumParticles, _numParticles);
        _renderer.SetPropertyBlock(_materialPropertyBlock);
    }
}
