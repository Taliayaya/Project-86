using UnityEngine;

namespace UI
{
    public class OpenWindowButton : MonoBehaviour
    {
        [SerializeField] private Transform window;
        public void OpenWindow()
        {
            WindowManager.Open(() => window.gameObject.SetActive(true), () => window.gameObject.SetActive(false));
        }
        
        public void CloseWindow()
        {
            WindowManager.Close();
        }
        
        public void CloseAllWindows()
        {
            WindowManager.CloseAll();
        }
    }
}