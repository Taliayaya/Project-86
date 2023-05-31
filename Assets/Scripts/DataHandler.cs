using ScriptableObjects.GameParameters;
using UnityEngine;

namespace DefaultNamespace
{
    public static class DataHandler
    {
        /// <summary>
        /// Fetch all the game data from the resources folder and save its content
        /// </summary>
        public static void SaveGameData()
        {
            var gameParametersArray = Resources.LoadAll<GameParameters>("ScriptableObjects/Parameters");
            foreach (var parameters in gameParametersArray)
                parameters.SaveToFile();
        }
    }
}