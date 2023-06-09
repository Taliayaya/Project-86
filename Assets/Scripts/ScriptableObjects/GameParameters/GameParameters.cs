using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    /// <summary>
    /// GameParameter is the base class for all the parameters section of the game.
    /// It allows to dynamically show or hide fields in the game settings.
    /// </summary>
    public abstract class GameParameters : JsonSerializableSO
    {
        [SerializeField] protected List<string> fieldsToShowInGame = new List<string>();
        
        public abstract string GetParametersName { get; }
        
        /// <summary>
        /// Tells whether or not the field should be shown in the game.
        /// A field is shown if it is inside the list of fields to show.
        /// </summary>
        /// <param name="fieldName">The name to compare with the allowed fields</param>
        /// <returns>True if the field is inside the list</returns>
        public bool ShowsField(string fieldName)
        {
            return fieldsToShowInGame.Contains(fieldName);
        }
        
        /// <summary>
        /// Toggle the state of the field.
        /// If it is shown, it will be hidden, and vice versa.
        /// </summary>
        /// <param name="fieldName">The field to toggle</param>
        public void ToggleShowField(string fieldName)
        {
            if (ShowsField(fieldName))
                fieldsToShowInGame.Remove(fieldName);
            else
                fieldsToShowInGame.Add(fieldName);
        }
        
        public List<string> FieldsToShowInGame => fieldsToShowInGame;
        
    }
}