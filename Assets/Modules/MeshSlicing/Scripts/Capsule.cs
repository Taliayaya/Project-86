using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Capsule : MonoBehaviour
{
    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;

    private Vector3 _maxYVertex;
    private Vector3 _minYVertex;
    private Vector3 _maxXVertex;
    private Vector3 _minXVertex;

    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;

        _vertices = _mesh.vertices;
        _triangles = _mesh.triangles;

        //Get the minimum and maximum verteces
        _maxYVertex = _vertices.FirstOrDefault();

    }
 
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if(_vertices == null)
        {
            return;
        }

        int count = 0;
        foreach(Vector3 vertex in _vertices)
        {
            if(count==3)
            {
                count = 0;
            }

            var vert = new Vector3(transform.position.x + (vertex.x * transform.localScale.x), transform.position.y + (vertex.y * transform.localScale.y), transform.position.z + (vertex.z * transform.localScale.z));

            if(count==0)
            {
                Gizmos.color = Color.yellow;
            }
            if((count)==1)
            {
                Gizmos.color = Color.red;
            }
            else if ((count)==2)
            {
                Gizmos.color = Color.green;
            }
            else if((count)==3)
            {
                Gizmos.color = Color.blue;
            }

            Gizmos.DrawSphere(vert, 0.01f);

            count++;
        }
    }
}
