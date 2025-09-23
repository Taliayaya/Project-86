using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;

public class Sliceable : MonoBehaviour
{
    [SerializeField]
    private bool _isSolid = true;

    [SerializeField]
    private bool _reverseWindTriangles = false;

    [SerializeField]
    private bool _useGravity = false;

    [SerializeField]
    private bool _shareVertices = false;

    [SerializeField]
    private bool _smoothVertices = false;

    [SerializeField]
    private bool _canSlice = true;

    public bool IsSolid
    {
        get => _isSolid;
        set => _isSolid = value;
    }

    public bool ReverseWireTriangles
    {
        get => _reverseWindTriangles;
        set => _reverseWindTriangles = value;
    }

    public bool UseGravity 
    {
        get => _useGravity;
        set => _useGravity = value;
    }

    public bool ShareVertices 
    {
        get => _shareVertices;
        set => _shareVertices = value;
    }

    public bool SmoothVertices 
    {
        get => _smoothVertices;
        set => _smoothVertices = value;
    }
    public bool CanSlice
    {
        get => _canSlice;
        set => _canSlice = value;
    }

}
