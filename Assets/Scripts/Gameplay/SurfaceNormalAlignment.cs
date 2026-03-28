using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Analytics.IAnalytic;
using static UnityEngine.LightAnchor;

public class SurfaceNormalAlignment : NetworkBehaviour
{
    [SerializeField] private GameObject[] weightRaycasts;
    // Stabilizes surface normal jitter on uneven terrain
    [SerializeField] private float normalLerp = 0.4f;
    // Smooths alignment
    [SerializeField] private float upDirectionLerp = 0.1f;

    public float floorAngle = 0f;
    public Vector3 normal = Vector3.up; // takes into account hits only below discardAngle threshold
    public Vector3 rawNormal = Vector3.up; // takes into account all raycast hits
    public Vector3 upDirection = Vector3.up;
    public float groundDistance = 0f;
    public float discardAngle = 40f;

    public bool pause = false;
    public bool useRawNormal = false;

    private Vector3[] rayPositions;
    private Vector3[] rayDirections;
    private float[] rayWeights;
    private float[] rayLengths;


    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        CompileRaycasts();
    }

    private void CompileRaycasts()
    {
        if (true || IsOwner)
        {
            // Converts dummy raycast gameobjects into an ECS like data structure
            rayPositions = new Vector3[weightRaycasts.Length];
            rayDirections = new Vector3[weightRaycasts.Length];
            rayWeights = new float[weightRaycasts.Length];
            rayLengths = new float[weightRaycasts.Length];
            //Debug.Log(weightRaycasts.Length);

            for (int i = 0; i < weightRaycasts.Length; i++)
            {
                rayPositions[i] = weightRaycasts[i].transform.localPosition;
                rayDirections[i] = weightRaycasts[i].transform.forward;
                SurfaceAlignmentRaycast SAR =
                    weightRaycasts[i].GetComponent("SurfaceAlignmentRaycast") as SurfaceAlignmentRaycast;
                rayWeights[i] = SAR.weight;
                rayLengths[i] = SAR.length;
                //Debug.Log(i);
            }
        }

        for (int i = 0; i < weightRaycasts.Length; i++)
        {
            //Debug.Log(weightRaycasts[i].name);
            Destroy(weightRaycasts[i]);
        }

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (pause || !HasAuthority) { return; }
        ProcessFloorRays();

        AverageRaycasts();
        floorAngle = Vector3.Angle(Vector3.up, normal);

        AlignTransform();
    }

    private void AverageRaycasts()
    {
        Vector3[] normals = new Vector3[rayPositions.Length];
        Vector3[] rawNormals = new Vector3[rayPositions.Length];
        RaycastHit hitData;
        // Collects normals from raycasts that hit
        int hits = 0;
        int rawHits = 0;
        for (int i = 0; i < rayPositions.Length; i++)
        {
            if (Physics.Raycast(transform.position + rayPositions[i], transform.TransformDirection(rayDirections[i]), out hitData, rayLengths[i])) 
            {
                rawNormals[hits] = hitData.normal * rayWeights[i];
                rawHits++;
                if (Vector3.Angle(Vector3.up, hitData.normal) > discardAngle) { continue; }
                normals[hits] = hitData.normal * rayWeights[i];
                //Debug.Log(hitData.collider.name);
                hits++;
            }   
        }
        // Averages raycast results 
        // In case of no hits, previous normal is returned
        // since previous normal is used, it acts as a bit of smoothing in the changes? - requires testing
        
        for (int i = 0; i < hits; i++)
        {
            normal += normals[i];
        }
        for (int i = 0; i < rawHits; i++)
        {
            rawNormal += rawNormals[i];
        }
        normal = normal.normalized;
        rawNormal = rawNormal.normalized;

        if (rawHits == 0) { rawNormal = Vector3.up; }
        var rawAngle = Vector3.Angle(rawNormal, Vector3.up);
        //rawNormal = Vector3.Slerp(rawNormal, Vector3.up, Mathf.Max((rawAngle - 80) / rawAngle, 0)); // clamp rawNormal to ~80 degrees angle of global up direction
    }

    private void AlignTransform()
    {
        normal = Vector3.Slerp(transform.up, normal, normalLerp);
        rawNormal = Vector3.Slerp(transform.up, rawNormal, normalLerp);
        if (useRawNormal) {

            upDirection = Vector3.Slerp(upDirection, rawNormal, upDirectionLerp);
            //Debug.Log("RAW NORMALLLLL!");
        }
        else {upDirection = Vector3.Slerp(upDirection, normal, upDirectionLerp); }

        Vector3 right = Vector3.Cross(upDirection, transform.forward);
        Vector3 forward = Vector3.Cross(right, upDirection);
        transform.LookAt(transform.position + forward, upDirection);
    }
    private void ProcessFloorRays()
    {
        RaycastHit hitData;
        // ground distance relative to local down
        if (Physics.Raycast(transform.position, -transform.up, out hitData, 25.0f))
        {
            groundDistance = Vector3.Distance(transform.position, hitData.point);
        }
        // ground distance relative to global down
        else
        {
            Physics.Raycast(transform.position, Vector3.down, out hitData, 500.0f);
            groundDistance = Vector3.Distance(transform.position, hitData.point);
        }
    }
}
