using System;
using Gameplay.Quests.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

public class NetworkManager : Singleton<NetworkManager>
{
    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
}