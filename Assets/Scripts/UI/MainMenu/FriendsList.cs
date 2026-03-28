using System;
using Integrations;

namespace UI.MainMenu
{
    using UnityEngine;
    using Discord.Sdk;
    using System.Collections.Generic;

    public class FriendsList : MonoBehaviour
    {
        [SerializeField]
        GameObject friendUIPrefab;

        [SerializeField]
        Transform friendListContentTransform;

        List<GameObject> friendUIObjects = new List<GameObject>();

        private void OnEnable()
        {
            DiscordManager.Instance.onStatusChanged += InstanceOnonStatusChanged;
        }

        private void OnDisable()
        {
            DiscordManager.Instance.onStatusChanged -= InstanceOnonStatusChanged;
        }

        private void InstanceOnonStatusChanged(Client.Status obj)
        {
            if (obj == Client.Status.Ready)
                LoadFriends(DiscordManager.Instance.Client);
        }

        public void LoadFriends(Client client)
        {
            RelationshipHandle[] relationships = client.GetRelationshipsByGroup(RelationshipGroupType.OnlinePlayingGame);
            foreach (var relationship in relationships)
            {
                GameObject friendUI = Instantiate(friendUIPrefab, friendListContentTransform);
                friendUI.GetComponent<FriendUI>().Initialize(client, relationship);
                friendUIObjects.Add(friendUI);
            }

            SortFriends();
        }

        // Discord users can change their name or online status, use this to keep the UI up to date
        public void UpdateFriends()
        {
            foreach (var friendUIObject in friendUIObjects)
            {
                friendUIObject.GetComponent<FriendUI>().UpdateFriend();
            }
        }

        public void SortFriends()
        {
            // Sort friends by online status and then by display name
            friendUIObjects.Sort((a, b) =>
            {
                FriendUI friendA = a.GetComponent<FriendUI>();
                FriendUI friendB = b.GetComponent<FriendUI>();

                RelationshipHandle relationshipA = friendA.RelationshipHandle;
                RelationshipHandle relationshipB = friendB.RelationshipHandle;

                StatusType statusA = relationshipA.User().Status();
                StatusType statusB = relationshipB.User().Status();

                if (statusA != statusB)
                    return statusA.CompareTo(statusB);

                return relationshipA.User().DisplayName().CompareTo(relationshipB.User().DisplayName());
            });

            // Reorder the friend UI elements in the hierarchy after sorting
            for (int i = 0; i < friendUIObjects.Count; i++)
            {
                friendUIObjects[i].transform.SetSiblingIndex(i);
            }
        }
    }
}