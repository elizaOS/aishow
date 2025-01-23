using UnityEngine;

public class CameraArmPitchController : MonoBehaviour
{
   public Transform cameraArm;

   public float pitchMin = -10f;
   public float pitchMax = 10f;
   public float pitchDuration = 5f;

   private bool isPitchingUp = true;
   private float currentPitchTime = 0f;
   private float totalPitchTime;

   private void Start()
   {
       totalPitchTime = pitchDuration;
   }

   private void LateUpdate()
   {
       HandlePitching();
   }

   private void HandlePitching()
   {
       currentPitchTime += Time.deltaTime;
       float t = Mathf.Clamp01(currentPitchTime / totalPitchTime);
       t = Mathf.SmoothStep(0f, 1f, t);

       float pitch = Mathf.Lerp(
           isPitchingUp ? pitchMin : pitchMax, 
           isPitchingUp ? pitchMax : pitchMin, 
           t
       );

       cameraArm.localRotation = Quaternion.Euler(0f, 0f, pitch);

       if (t >= 1f)
       {
           isPitchingUp = !isPitchingUp;
           currentPitchTime = 0f;
       }
   }
}