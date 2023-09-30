    using System.Linq;
    using Gameplay.Quests;
    using Gameplay.Quests.Tasks;
    using Gameplay.Quests.Tasks.TaskHelper;
    using Gameplay.Quests.Tasks.TasksType;
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
                    var go = new GameObject();
                    var instance = (Task)go.AddComponent(type);
                    go.transform.parent = quest.transform;
                    instance.name = type.Name;
                    quest.Tasks.Add(instance);
                    instance.Owner = quest;

                    switch (instance)
                    {
                        case ReachTask reachTask:
                            var prefab = Resources.Load("Prefabs/Quests/Area/QuestArea");
                            var reachZone = Instantiate(prefab) as GameObject;
                            reachZone.transform.parent = go.transform;
                            var reachArea = reachZone.GetComponent<ReachZone>();
                            reachTask.zoneArea = reachArea;
                            reachZone.name = "ReachZone";
                            break;
                    }
                }
            }
        }
        
    }