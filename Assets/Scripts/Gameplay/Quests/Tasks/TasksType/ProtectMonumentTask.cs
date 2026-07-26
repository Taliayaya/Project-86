using Gameplay;
using UnityEngine;

namespace Gameplay.Quests.Tasks.TasksType
{
    /// <summary>
    /// Shows "Protect MONUMENT" on the quest list. Fails when its monument is destroyed.
    /// Completes automatically when the owning quest completes (monument survived).
    /// Set importance to Optional: losing one monument is tolerated, but when the LAST
    /// monument falls the task promotes itself to Principal, which makes
    /// Quest.NotifyTaskStatusChanged cancel the whole quest (mission fail).
    /// </summary>
    public class ProtectMonumentTask : Task
    {
        [SerializeField] private Monument monument;

        public override void Activate()
        {
            base.Activate();
            if (monument.IsDestroyed)
                Fail();
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            monument.onDestroyed.AddListener(Fail);
        }

        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            monument.onDestroyed.RemoveListener(Fail);
        }

        public override void Fail()
        {
            // ponytail: promote instead of a separate fail-watcher task — Quest already
            // cancels itself when a Principal task fails, and a watcher would show up
            // as an extra line in the QuestWindow HUD.
            if (Monument.AllDestroyed)
                importance = TaskImportance.Principal;
            base.Fail();
        }

        public override string ToString() => $"Protect {monument.monumentName}";
    }
}
