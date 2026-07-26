using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Utility
{
    [RequireComponent(typeof(Slider))]
    public class SliderUtils : MonoBehaviour
    {
        private Slider _slider;
        
        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        public void GoTo0In(float second)
        {
            DOTween.To(() => _slider.value, x => _slider.value = x, 0, second).SetEase(Ease.Linear);
        }
        
        public void GoTo1In(float second)
        {
            DOTween.To(() => _slider.value, x => _slider.value = x, 1, second).SetEase(Ease.Linear);
        }
        
        public void GoTo0In(GameObject _, float second) => GoTo0In(second);
    }
}