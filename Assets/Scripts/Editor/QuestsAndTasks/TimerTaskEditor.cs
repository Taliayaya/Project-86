    using System.Linq;
    using Gameplay.Quests.Tasks;
    using Gameplay.Quests.Tasks.TaskHelper.Timer;
    using Gameplay.Quests.Tasks.TasksType;
    using ScriptableObjects.Quests;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(TimerTask))]
    public class TimerTaskEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var task = (TimerTask) target;
            var types = TypeCache.GetTypesDerivedFrom<TimerOverride>();
            foreach (var type in types)
            {
                if (GUILayout.Button($"Add {type.Name}"))
                {
                    var instance = (TimerOverride)CreateInstance(type);
                    instance.name = type.Name;
                    task.timerOverrides.Add(instance);
                    instance.Task = task;
                    AssetDatabase.AddObjectToAsset(instance, task);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }