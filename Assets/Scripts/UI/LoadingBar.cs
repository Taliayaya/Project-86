using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LoadingBar : MonoBehaviour
    {
        [SerializeField] private Image loadingBar;
        
        private void Update()
        {
            loadingBar.fillAmount = SceneHandler.LoadProgress();
        }
    }
}