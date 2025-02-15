using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Gameplay;
using UI.HUD;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

public class ArtilleryStrike : MonoBehaviour
{
    [Header("Artillery setup")]
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private GameObject _visualEffect;
    [SerializeField] private LayerMask damageLayer;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private Vector3 explosionVelocity;
    
    [Header("Prevention setup")]
    [SerializeField] private Transform minimapIcon;
    [SerializeField] private DecalProjector zoneDecalProjector ;
    
    [Header("Artillery Settings")]
    [SerializeField] private float strikesPerSecond;
    [SerializeField] private float strikeDuration;
    [SerializeField] private float strikeRadius;
    
    [Header("Damage Package")]
    [SerializeField] private AnimationCurve damageCurve;
    [SerializeField] private float damageRadius;
    
    private List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();
    private Collider[] _collidersBuffer = new Collider[50];
    
    private void OnParticleCollision(GameObject other)
    {
        int numCollisions = _particleSystem.GetCollisionEvents(other, _collisionEvents);
        Debug.Log($"Number of collisions: {numCollisions}");
        for (int i = 0; i < numCollisions; i++)
        {
            ParticleCollisionEvent collision = _collisionEvents[i];
            Explode(collision.intersection);
        }
    }
    
    private void Explode(Vector3 position)
    {
        Instantiate(_visualEffect, position, Quaternion.identity);
        impulseSource.GenerateImpulseAt(position, explosionVelocity);
        var size = Physics.OverlapSphereNonAlloc(position, damageRadius, _collidersBuffer, damageLayer);
        for (int i = 0; i < size; i++)
        {
            ApplyDamage(position, _collidersBuffer[i]);
        }
    }
    
    private void WarnPlayer(Vector3 center, float damageZoneRadius)
    {
        Debug.Log("Warning player: " + center + " " + damageZoneRadius);
        Vector3 playerPosition = PlayerManager.PlayerPosition;
        playerPosition.y = 0;
        center.y = 0;
        if (Vector3.Distance(PlayerManager.PlayerPosition, center) < damageZoneRadius)
        {
            EventManager.TriggerEvent(Constants.TypedEvents.ShowHUDWarning, new HUDWarningData()
            {
                main = "Skorpion Strike Incoming",
                sub = "Evacuate to a safe area or take cover",
                duration = 3f
            });
        }
    }

    private void ApplyDamage(Vector3 origin, Collider target)
    {
        Debug.Log($"Try Applying damage to {target.name}");
        if (!target.TryGetComponent(out IHealth health))
            return;
        Debug.Log($"Applying damage to {target.name}");
        float distance = Vector3.Distance(origin, target.transform.position);
        DamagePackage damagePackage = new DamagePackage()
        {
            Faction = Faction.Neutral,
            DamageAmount = damageCurve.Evaluate(distance / damageRadius),
            DamageSourcePosition = origin,
            IsBullet = false,
        };
        health.TakeDamage(damagePackage);
    }

    // 100y = 100x => y = x
    // 100y = 200x => y = 2x
    // 100y = 25x => y = 1/4x
    IEnumerator StrikePrevention(float delay)
    {
        float remainingTime = delay;
        float damageZoneSize = strikeRadius * Mathf.Abs(_particleSystem.velocityOverLifetime.x.constantMax);
        bool warnPlayer = true;
        while (remainingTime > 0)
        {
            if (warnPlayer && remainingTime < delay * 2 / 3)
            {
                warnPlayer = false;
                WarnPlayer(transform.position, damageZoneSize);
            }
            remainingTime -= Time.fixedDeltaTime;
            float zoneSize = damageZoneSize * (delay - remainingTime) / delay;
            minimapIcon.localScale = Vector3.one * (zoneSize / 4);
            zoneDecalProjector.size = new Vector3(2 * zoneSize, 2 * zoneSize, zoneDecalProjector.size.z);
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator StrikeExecution(float strikeDuration_, float strikesPerSecond_, float strikeRadius_, float delay)
    {
        yield return StartCoroutine(StrikePrevention(delay));
        _particleSystem.Stop();
        var main = _particleSystem.main;
        main.duration = strikeDuration_;
        var emission = _particleSystem.emission;
        emission.rateOverTime = strikesPerSecond_;
        var shape = _particleSystem.shape;
        shape.radius = strikeRadius_;
        _particleSystem.Play();
    }

    public void SendStrike(float strikeDuration_, float strikesPerSecond_, float strikeRadius_, float delay)
    {
        StartCoroutine(StrikeExecution(strikeDuration_, strikesPerSecond_, strikeRadius_, delay));
    }
    
    [ContextMenu("Send Strike")]
    public void SendStrike()
    {
        SendStrike(strikeDuration, strikesPerSecond, strikeRadius, 0);
    }
    
    public void DestroyAfter(float time)
    {
        Destroy(gameObject, time);
    }

}
