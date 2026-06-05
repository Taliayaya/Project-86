using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class Intersection : MonoBehaviour
    {
        [Serializable]
        public struct Branch
        {
            public int splineId;
            public string name;
            public List<string> hints;
        }
        public List<Branch> branches;
        public MorphoIntersectionChannel intersectionChannel;
        
        public void SendIntersectionData()
        {
            Debug.Log("Sending intersection data");
            intersectionChannel.SendEventMessage(branches);
        }

    }
}