using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public abstract class SettingsMenuCategory : MonoBehaviour
    {
        [SerializeField] protected GameObject menuCategoryPrefab;
        public virtual string MenuContentName { get; set; } = "Menu Category";
        

        public abstract void GenerateContent(Transform contentParent);

        public void CreateMenu(SettingsUI settingsUI)
        {
            CreateMenu(settingsUI, MenuContentName);
        }
        public void CreateMenu(SettingsUI settingsUI, string menuContentName)
        {
            var menuButton = Instantiate(menuCategoryPrefab, settingsUI.menusParent);
            menuButton.GetComponentInChildren<TMP_Text>().text = menuContentName;
            menuButton.GetComponent<Button>().onClick.AddListener(() => settingsUI.GenerateMenuCategory(this));
        }

        public abstract void OnMenuCategoryGenerated(SettingsUI settingsUI);
        
        public abstract void ResetSettings(SettingsUI settingsUI);
    }
}