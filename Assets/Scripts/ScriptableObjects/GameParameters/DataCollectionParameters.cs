using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "DataCollectionParameters", menuName = "Scriptable Objects/GameParameters/DataCollectionParameters")]
    public class DataCollectionParameters : GameParameters
    {
        public enum DataCollectionAgreement
        {
            Agreed,
            Disagreed,
            NotAsked
        }
        public DataCollectionAgreement agreement = DataCollectionAgreement.NotAsked;
        public bool deleteDataOnDisagreement = false;
        public override string GetParametersName => "Data Collection Settings";
    }
}