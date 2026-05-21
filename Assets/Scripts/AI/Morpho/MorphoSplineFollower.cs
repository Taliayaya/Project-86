using NaughtyAttributes;
using Unity.Cinemachine;

namespace AI
{
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class MorphoSplineFollower : MonoBehaviour
{
    [Header("Configuration du Chemin")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float minSpeed = 2f;
    
    [Header("Paramètres de Virage")]
    [Tooltip("Sensibilité au virage : plus c'est haut, plus il ralentit vite.")]
    [SerializeField] private float curveSensitivity = 50f;
    [SerializeField] private float speedSmoothTime = 0.5f;
    [SerializeField] private float lookAheadDistance = 15f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingBoolName = "isWalking";
    [SerializeField] private float clipMaxSpeed = 4;
    
    [Header("Vibration")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float vibrationStrength = 1f;
    [SerializeField] private float vibrationRate = .5f;

    private double distanceTraveled;
    private float nativeSplineLength;
    [ShowNonSerializedField] private float currentSpeed;
    public float CurrentSpeed => currentSpeed;
    public float MaxSpeed => maxSpeed;
    private float targetSpeed;
    private float speedVelocity;

    void Start()
    {
        if (splineContainer == null) return;
        nativeSplineLength = splineContainer.CalculateLength();
        currentSpeed = maxSpeed;
        
        if (animator != null)
        {
            animator.SetBool(walkingBoolName, true);
        }
        
        InvokeRepeating(nameof(VibrateGround), 0, 0.5f);
    }

    void Update()
    {
        if (splineContainer == null) return;

        // 1. Calcul de la courbure pour adapter la vitesse
        float t = (float)(distanceTraveled / nativeSplineLength) % 1f;
        AdjustSpeedBasedOnCurvature(t);

        // 2. Application du mouvement
        distanceTraveled += currentSpeed * Time.deltaTime;
        float3 position = splineContainer.EvaluatePosition(t);
        transform.position = position;

        // 3. Rotation
        float3 forward = splineContainer.EvaluateTangent(t);
        if (!forward.Equals(float3.zero))
        {
            transform.rotation = Quaternion.LookRotation(forward, splineContainer.EvaluateUpVector(t));
        }

        // 4. Update Animator (Optionnel : ralentit aussi l'anim de marche)
        if (animator != null)
        {
            animator.speed = currentSpeed / maxSpeed * clipMaxSpeed; 
        }
    }

    void VibrateGround()
    {
        impulseSource.GenerateImpulseAt(transform.position, (vibrationStrength * currentSpeed) * Vector3.up);
    }

    void AdjustSpeedBasedOnCurvature(float t)
    {
        // On convertit le 't' (0 à 1) en distance réelle (mètres)
        float currentDistance = t * nativeSplineLength;
    
        // On regarde 10 ou 15 mètres plus loin (fixe)
        float nextT = ((currentDistance + lookAheadDistance) % nativeSplineLength) / nativeSplineLength;

        float3 tangentA = splineContainer.EvaluateTangent(t);
        float3 tangentB = splineContainer.EvaluateTangent(nextT);

        // Calcul de l'angle
        float angle = Vector3.Angle(tangentA, tangentB);

        // DEBUG : Décommente la ligne dessous pour voir l'angle dans la console
        // Debug.Log($"Angle: {angle:F2} | Vitesse: {currentSpeed:F1}");

        targetSpeed = Mathf.Lerp(maxSpeed, minSpeed, angle * curveSensitivity);
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);
    }
}
}