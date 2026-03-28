using System;
using UnityEngine;

namespace FMODUnity
{
    [Obsolete("Use the EventReference struct instead")]
    public class EventRefAttribute : PropertyAttribute
    {
        public string MigrateTo = null;
    }
}
