using UnityEngine;

public class JibCameraController : MonoBehaviour
{
   public Transform jibArm; // The pivot point for left-right rotation

   public float panAngle = 180f; // Total angle for left-to-right motion
   public float panSpeed = 2f; // Speed of panning

   private bool isPanningRight = true; // Direction of panning
   private float currentPanTime = 0f; // Timer for panning
   private float totalPanTime; // Calculated total time for the pan

   private void Start()
   {
       // Calculate total pan time based on speed and distance
       totalPanTime = panAngle / panSpeed;

       // Set initial arm rotation
       jibArm.localRotation = Quaternion.Euler(0f, -panAngle / 2f, 0f);
   }

   private void LateUpdate()
   {
       HandlePanning();
   }

   private void HandlePanning()
   {
       // Smoothly interpolate between start and end rotation
       currentPanTime += Time.deltaTime;
       float t = Mathf.Clamp01(currentPanTime / totalPanTime);
       t = Mathf.SmoothStep(0f, 1f, t); // Ease in and out

       // Use direct angle interpolation instead of Quaternion
       float currentAngle = Mathf.Lerp(
           isPanningRight ? -panAngle / 2f : panAngle / 2f, 
           isPanningRight ? panAngle / 2f : -panAngle / 2f, 
           t
       );

       jibArm.localRotation = Quaternion.Euler(0f, currentAngle, 0f);

       // Check if panning has completed
       if (t >= 1f)
       {
           isPanningRight = !isPanningRight;
           currentPanTime = 0f;
       }
   }
}