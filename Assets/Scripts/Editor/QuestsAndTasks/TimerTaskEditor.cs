    using System.Linq;
    using Gameplay.Quests.Tasks;
    using Gameplay.Quests.Tasks.TaskHelper.Timer;
    using Gameplay.Quests.Tasks.TasksType;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(TimerTask))]
    public class TimerTaskEditor : UnityEditor.Editor
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
                    var go = new GameObject();
                    var instance = (TimerOverride)go.AddComponent(type);
                    go.transform.parent = task.transform;
                    instance.name = type.Name;
                    task.timerOverrides.Add(instance);
                    instance.Task = task;
                }
            }
        }
    }