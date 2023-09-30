using Gameplay.Quests.Tasks;
using Gameplay.Quests.Tasks.TaskHelper;
using Gameplay.Quests.Tasks.TaskHelper.TasksModules;
using Gameplay.Quests.Tasks.TasksType;
using UnityEditor;
using UnityEngine;

namespace Editor.QuestsAndTasks
{
    [CustomEditor(typeof(Task), true)]
    public class TaskEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var task = (Task)target;
            var types = TypeCache.GetTypesDerivedFrom<TaskModule>();
            foreach (var type in types)
            {
                if (GUILayout.Button($"Add {type.Name}"))
                {
                    var go = new GameObject();
                    var instance = (TaskModule)go.AddComponent(type);
                    go.transform.parent = task.transform;
                    instance.name = type.Name;
                }
            }
        }
    }
}