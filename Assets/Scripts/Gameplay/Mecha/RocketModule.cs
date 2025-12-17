using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Units;
using Managers;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Mecha
{
    [Serializable]
    public class RocketSalve
    {
        public List<Transform> launchPoints;
        public List<GameObject> visual;
        public List<bool> used;

        public int LaunchPointsCount => launchPoints.Count;

        public bool IsUsed(int i) => used[i];
        public void Use(int i)
        {
            used[i] = true;
            visual[i].SetActive(false);
        }

        public bool IsEmpty() => used.TrueForAll(u => u);

        public void Reload()
        {
            for (var i = 0; i < used.Count; i++)
            {
                used[i] = false;
                visual[i].SetActive(true);
            }
        }
    }

    public enum RocketStrategy
    {
        None,
        Random,
        Preshot,
        PreshotRandom
    }
    public class RocketModule : Module
    {
        public GameObject rocketPrefab;
        public Transform target;
        public NetworkAnimator animator;
        
        [Header("Events")]
        public UnityEvent onRocketLaunch;

        [Header("Settings")] 
        public List<RocketSalve> rocketSalves;
        public float timeBetweenRockets = 0.3f;
        public RocketStrategy rocketStrategy = RocketStrategy.None;
        public float preshotTime = 1f;
        public float randomSize = 1f;
        public float reloadDuration = 10f;

        private int _currentSalve;
        private int _currentRocket;

        private bool SalveLaunchRocket(RocketSalve salve, int rocket)
        {
            if (rocket >= salve.LaunchPointsCount) return false;
            if (salve.IsUsed(rocket)) return false;
            
            salve.Use(rocket);
            return true;
        }

        public bool NextSalve()
        {
            _currentSalve++;
            _currentRocket = 0;
            if (_currentSalve >= rocketSalves.Count) _currentSalve = 0;
            return !rocketSalves[_currentSalve].IsEmpty();
        }

        [ContextMenu("Launch Rocket")]
        public void LaunchNextRocket()
        {
            RocketSalve salve = rocketSalves[_currentSalve];
            bool launched = SalveLaunchRocket(salve, _currentRocket);
            if (!launched && NextSalve())
            {
                LaunchNextRocket();
                return;
            }
            
            Vector3 targetPosition = ComputeIdealTargetPosition(target);
            LaunchRpc(_currentSalve, _currentRocket, targetPosition);
            _currentRocket++;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void LaunchRpc(int salveIndex, int rocketIndex, Vector3 targetPosition)
        {
            RocketSalve salve = rocketSalves[salveIndex];
            Launch(salve.launchPoints[rocketIndex], targetPosition);
        }
        

        public void Launch(Transform origin, Vector3 targetPosition)
        {
            var rocket = PoolManager.Instance.Instantiate(rocketPrefab, origin.position, Quaternion.LookRotation(origin.forward));
            rocket.GetComponent<RocketController>().Launch(origin.forward, targetPosition);
            onRocketLaunch?.Invoke();
        }

        private int _launchSalve;
        [ContextMenu("Launch Salve")]
        public void LaunchSalve()
        {
            LaunchSalveRpc(_launchSalve);
            _launchSalve = (_launchSalve + 1) % rocketSalves.Count;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void LaunchSalveRpc(int salveIndex)
        {
            StartCoroutine(LaunchSalveRoutine(rocketSalves[salveIndex]));
        }

        [ContextMenu("Reload Salves")]
        public void ReloadAllSalves()
        {
            ReloadAllSalvesRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ReloadAllSalvesRpc()
        {
            foreach (var rocketSalve in rocketSalves)
            {
                rocketSalve.Reload();
            }
        }

        IEnumerator LaunchSalveRoutine(RocketSalve salve)
        {
            for (int i = 0; i < salve.LaunchPointsCount; i++)
            {
                // if the target got destroyed, stop launching rockets
                if (target == null)
                    break;
                Vector3 targetPosition = ComputeIdealTargetPosition(target);
                if (SalveLaunchRocket(salve, i))
                {
                    Launch(salve.launchPoints[i], targetPosition);
                    yield return new WaitForSeconds(timeBetweenRockets);
                }
            }
        }

        public Vector3 ComputeIdealTargetPosition(Transform aimedTarget)
        {
            Vector3 targetPos = aimedTarget.position;
            Vector3 randomOffset = Vector3.zero;
            if (rocketStrategy == RocketStrategy.Random || rocketStrategy == RocketStrategy.PreshotRandom)
            {
                randomOffset = UnityEngine.Random.insideUnitSphere * randomSize;
            }

            if (rocketStrategy == RocketStrategy.Preshot || rocketStrategy == RocketStrategy.PreshotRandom)
            {
                if (aimedTarget.TryGetComponent(out Rigidbody rb))
                    targetPos = rb.position + rb.linearVelocity * preshotTime;
            }
            
            return targetPos + randomOffset;
        }

        [ContextMenu("LauncherOpen")]
        public void LauncherOpen()
        {
            animator.SetTrigger("LauncherOpen");
        }
        
        [ContextMenu("LauncherClose")]
        public void LauncherClose()
        {
            animator.SetTrigger("LauncherClose");
        }

        public bool IsReloading { get; private set; }

        [ContextMenu("Reload")]
        public void Reload()
        {
            if (IsReloading)
                return;
            
            StartCoroutine(ReloadCoroutine());
        }

        public IEnumerator ReloadCoroutine()
        {
            IsReloading = true;
            LauncherClose();
            yield return new WaitForSeconds(reloadDuration);
            ReloadAllSalves();
            IsReloading = false;
        }
        
        public bool IsEmpty() => rocketSalves.TrueForAll(salve => salve.IsEmpty());
        
        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public void SetTarget(TargetInfo info)
        {
            if (info == null || info.Unit == null)
                target = null;
            else
                target = info.Unit.transform;
        }
        
    }
}