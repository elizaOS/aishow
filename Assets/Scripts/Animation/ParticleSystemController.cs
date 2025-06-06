using UnityEngine;

public class ParticleSystemController : MonoBehaviour
{
    private ParticleSystem ps; // Renamed to avoid conflict with inherited 'particleSystem' member

    // Start is called before the first frame update
    void Start()
    {
        // Get the Particle System component attached to the same GameObject
        ps = GetComponent<ParticleSystem>();
    }

    // Toggle the particle system on and off
    public void ToggleParticles(bool isActive)
    {
        if (ps != null)
        {
            if (isActive)
                ps.Play();  // Play the particle system
            else
                ps.Stop();  // Stop the particle system
        }
    }

    // Trigger a burst of particles
    public void TriggerBurst(int count)
    {
        if (ps != null)
        {
            // Emit a burst of particles
            var main = ps.main;

            // Set up an EmitParams object for more control over emission
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            
            // You can also customize emitParams properties here if necessary, e.g., position, rotation

            // Emit the burst of particles (for example, emitting 'count' particles)
            ps.Emit(emitParams, count);
        }
    }

    // Stop the particle system with a fade-out over time
    public void StopWithFade(float fadeDuration)
    {
        if (ps != null)
        {
            var main = ps.main;
            main.startLifetime = Mathf.Lerp(main.startLifetime.constant, 0f, fadeDuration);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
