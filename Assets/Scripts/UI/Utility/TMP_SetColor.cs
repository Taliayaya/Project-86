using TMPro;
using UnityEngine;

namespace UI.Utility
{
    public class TMP_SetColor : MonoBehaviour
    {
        [SerializeField] private TMP_Text textInput;
        [SerializeField] private Color color;
        public void SetColor()
        {
            textInput.color = color;
        }
        
    }
}