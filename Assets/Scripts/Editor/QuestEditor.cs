    using System.Linq;
    using Gameplay.Quests;
    using Gameplay.Quests.Tasks;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(Quest))]
    public class QuestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var quest = (Quest) target;
            var types = TypeCache.GetTypesDerivedFrom<Task>();
            foreach (var type in types)
            {
                if (GUILayout.Button($"Add {type.Name}"))
                {
                    var instance = (Task)CreateInstance(type);
                    instance.name = type.Name;
                    quest.Tasks.Add(instance);
                    instance.Owner = quest;
                    AssetDatabase.AddObjectToAsset(instance, quest);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }