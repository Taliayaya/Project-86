
using UnityEngine;

[ExecuteAlways]
public class BezierDeformationController : MonoBehaviour
{
    [SerializeField] GameObject BeziezP0;
    [SerializeField] GameObject BeziezP1;
    [SerializeField] GameObject BeziezP2;

    
    [SerializeField] GameObject P0Origine;
    [SerializeField] GameObject P1Origine;
    [SerializeField] GameObject P2Origine;

    [SerializeField] Renderer TargetRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private static readonly int P0 = Shader.PropertyToID("_P0");
    private static readonly int P1 = Shader.PropertyToID("_P1");
    private static readonly int P2 = Shader.PropertyToID("_P2");

    private MaterialPropertyBlock propertyBlock;



    void Start()
    {
    }

    private void OnEnable()
    {
        ApplyShaderValues();
    }

    private void OnValidate()
    {

        ApplyShaderValues();
    }

    private void Update()
    {
        // Runs in edit mode too because of [ExecuteAlways]
        // Useful if you move the object or want live refresh

        ApplyShaderValues();
    }
    

    private void ApplyShaderValues()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        if (TargetRenderer == null ||
            P0Origine == null || P1Origine == null || P2Origine == null ||
            BeziezP0 == null || BeziezP1 == null || BeziezP2 == null)
            return;

        TargetRenderer.GetPropertyBlock(propertyBlock);

        // Vector3 deltaP0 = P0Origine.transform.position - BeziezP0.transform.position;
        // Vector3 deltaP1 = P1Origine.transform.position - BeziezP1.transform.position;
        // Vector3 deltaP2 = P2Origine.transform.position - BeziezP2.transform.position;

        // deltaP0 = transform.InverseTransformDirection(deltaP0) / 7.5f;
        // deltaP1 = transform.InverseTransformDirection(deltaP1) / 5f;
        // deltaP2 = transform.InverseTransformDirection(deltaP2) / 7.5f;

        propertyBlock.SetVector(P0, BeziezP0.transform.localPosition);
        propertyBlock.SetVector(P1, BeziezP1.transform.localPosition);
        propertyBlock.SetVector(P2, BeziezP2.transform.localPosition);

        TargetRenderer.SetPropertyBlock(propertyBlock);
    }
    // private void ApplyShaderValues()
    // {
    //
    //         
    //     // if (TargetRenderer == null)
    //     //     return;
    //
    //     if (propertyBlock == null)
    //         propertyBlock = new MaterialPropertyBlock();
    //
    //     TargetRenderer.GetPropertyBlock(propertyBlock);
    //     
    //     Vector3 vector3P0 = P0Origine.transform.position - BeziezP0.transform.position;
    //     Vector3 vector3P1 = P1Origine.transform.position - BeziezP1.transform.position; 
    //     Vector3 vector3P2 = P2Origine.transform.position - BeziezP2.transform.position; 
    //
    //     vector3P0 = new Vector3(vector3P0.y, vector3P0.z * -1, vector3P0.x) / 7.5f;
    //     vector3P1 = new Vector3(vector3P1.y, vector3P1.z * -1, vector3P1.x) / 5;
    //     vector3P2 = new Vector3(vector3P2.y, vector3P2.z * -1, vector3P2.x) / 7.5f;
    //
    //     propertyBlock.SetVector(P0, vector3P0);
    //     propertyBlock.SetVector(P1, vector3P1);
    //     propertyBlock.SetVector(P2, vector3P2);
    //
    //     TargetRenderer.SetPropertyBlock(propertyBlock);
    //     
    //         
    // }
}