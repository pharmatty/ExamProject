using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemScript : MonoBehaviour
{
    private ParticleSystem particle;
    private ParticleSystemRenderer particleRenderer;
    private Material particleMaterial;

    void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        particleRenderer = GetComponent<ParticleSystemRenderer>();
        particleMaterial = particleRenderer.material;
    }

    public void PlayParticleAtPosition(Vector3 pos)
    {
        var main = particle.main;

        transform.position = pos;
        particle.Play();

        particleMaterial.DOFade(1, 0);
        particleMaterial.DOFade(0, main.startLifetime.constant).SetEase(Ease.InExpo);
    }
}