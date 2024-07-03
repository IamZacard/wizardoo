using UnityEngine;

public class ParticleFollowPath : MonoBehaviour
{
    public LineRenderer pathLine;
    private new ParticleSystem particleSystem; // Use 'new' keyword here
    private ParticleSystem.Particle[] particles;

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
    }

    private void LateUpdate()
    {
        int aliveParticles = particleSystem.GetParticles(particles);

        for (int i = 0; i < aliveParticles; i++)
        {
            float timeAlive = particles[i].startLifetime - particles[i].remainingLifetime;
            float pathProgress = timeAlive / particles[i].startLifetime;

            particles[i].position = GetPositionOnPath(pathProgress);
        }

        particleSystem.SetParticles(particles, aliveParticles);
    }

    private Vector3 GetPositionOnPath(float progress)
    {
        float totalLength = pathLine.positionCount - 1;
        float scaledProgress = progress * totalLength;

        int index = Mathf.FloorToInt(scaledProgress);
        float t = scaledProgress - index;

        if (index >= pathLine.positionCount - 1)
        {
            return pathLine.GetPosition(pathLine.positionCount - 1);
        }

        Vector3 startPosition = pathLine.GetPosition(index);
        Vector3 endPosition = pathLine.GetPosition(index + 1);

        return Vector3.Lerp(startPosition, endPosition, t);
    }
}
