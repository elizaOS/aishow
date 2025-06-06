using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParticleSystemController))]
public class ParticleSystemControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default inspector for the class

        ParticleSystemController controller = (ParticleSystemController)target;

        // Add a toggle button to activate or deactivate the particle system
        if (GUILayout.Button("Toggle Particle System"))
        {
            bool isActive = !controller.GetComponent<ParticleSystem>().isPlaying;
            controller.ToggleParticles(isActive);
        }

        // Add a button to trigger a burst of particles
        if (GUILayout.Button("Trigger Particle Burst"))
        {
            controller.TriggerBurst(10); // Example: Trigger a burst of 10 particles
        }
    }
}
