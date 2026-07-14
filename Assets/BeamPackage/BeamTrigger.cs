using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using System.Collections;
using DG.Tweening;
using Gameplay;
using Unity.Cinemachine;
using Unity.Netcode.Components;
using UnityEngine.Rendering;

public class BeamTrigger : MonoBehaviour
{
    [SerializeField] VisualEffect beamChargingVFX;
    [SerializeField] VisualEffect beamTravelVFX;
    [SerializeField] VisualEffect explosionVFX;
    [SerializeField] Transform ExplosionPoint;
    [SerializeField] float beamChargeTime = 0.5f;
    [SerializeField] float beamTravelTime = 0.4f;
    [SerializeField] float explosionDuration = 3f;
    [SerializeField] private float explosionRadius = 200;
    [SerializeField] private int maxAllowedCollisions = 50;
    [SerializeField] private Animator animator;
    [Header("Wings")]
    [SerializeField] private float wingDowntime = 3f;

    [Header("Flare")] [SerializeField] private Light flareLight;
    [SerializeField] private LensFlareComponentSRP lensFlare;
    [SerializeField] private float flareChargingIntensity = 1;
    [SerializeField] private float flareChargingScale = 1;
    [SerializeField] private float flareLightChargingIntensity = 1;
    [SerializeField] private float flareTransitionDuration = 1f;

    [SerializeField] private float flarePrepareShootIntensity = 2;
    [SerializeField] private float flarePrepareShootScale = 1;

    [SerializeField] private float flareLightPrepareShootIntensity = 2;
    [SerializeField] private float flarePrepareTransitionDuration = 1f;
    
    [Header("Sound")]
    [SerializeField] private AudioClip chargingSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Vector2 pitch = new (0.8f, 1.5f);
    
    [Header("Rumble")]
    [SerializeField] private CinemachineImpulseSource rumbleSource;
    [SerializeField] private float rumbleIntensity = 0.5f;
    [SerializeField] private float rumbleDuration = 0.2f;
    [SerializeField] private float rumbleMaxDistance = 1000;
    
    private Vector3 _explosionScale;

    private Tween _scaleTween;
    private Tween _intensityTween;
    private Tween _lightTween;

    public bool InProgress { get; private set; }
    public bool Charging { get; private set; }
    private Coroutine _shootCoroutine;

    private void Start()
    {
        explosionVFX.transform.SetParent(null, true);
        explosionVFX.transform.rotation = Quaternion.identity;
        _explosionScale = explosionVFX.transform.localScale;
        _colliders = new Collider[maxAllowedCollisions];
    }

    private void SetAnimatorTrigger(string trigger)
    {
        animator.SetTrigger(trigger);
    }

    private void ChargingAudio()
    {
        audioSource.clip = chargingSound;
        audioSource.loop = true;
        audioSource.Play();

        audioSource.pitch = pitch.x;
        DOTween.To(() => audioSource.pitch , v => audioSource.pitch = v, pitch.y, beamChargeTime);
    }

    private IEnumerator RumbleDelay()
    {
        var explosionPosition = ExplosionPoint.position;
        var playerPosition = PlayerManager.Instance.transform.position;
        const float speedOfSound = 300;
        
        var distance = Vector3.Distance(explosionPosition, playerPosition);
        if (distance > rumbleMaxDistance) yield break;
        var time = distance / speedOfSound;
        yield return new WaitForSeconds(time);
        rumbleSource.GenerateImpulseAt(explosionPosition, rumbleIntensity * Vector3.down);
    }

    private Collider[] _colliders;
    IEnumerator ChargingBeam()
    {
        Charging = true;
        SetAnimatorTrigger("PrepareShoot");
        ChargingAudio();
        _scaleTween = DOTween.To(() => lensFlare.scale, v => lensFlare.scale = v, flareChargingScale, flareTransitionDuration * _power);

        _intensityTween = DOTween.To(() => lensFlare.intensity, v => lensFlare.intensity = v, flareChargingIntensity, flareTransitionDuration * _power);
        _lightTween = DOTween.To(() => flareLight.intensity, v => flareLight.intensity = v, flareChargingIntensity, flareTransitionDuration * _power);

        
        yield return new WaitForSeconds(beamChargeTime * _power);
        
        SetAnimatorTrigger("Shoot");
        _scaleTween = DOTween.To(() => lensFlare.scale, v => lensFlare.scale = v, flarePrepareShootScale, flarePrepareTransitionDuration);
        _intensityTween = DOTween.To(() => lensFlare.intensity, v => lensFlare.intensity = v, flarePrepareShootIntensity, flarePrepareTransitionDuration);
        _lightTween = DOTween.To(() => flareLight.intensity, v => flareLight.intensity = v, flareLightPrepareShootIntensity, flarePrepareTransitionDuration);

        yield return new WaitForSeconds(wingDowntime);
        Charging = false;
        beamTravelVFX.SetVector3("StartPosition", transform.position);
        beamTravelVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        beamTravelVFX.SendEvent("Trigger");
        beamChargingVFX.SendEvent("StopEffect");
        audioSource.Stop();
        yield return  new WaitForSeconds(beamTravelTime * _power);
        flareLight.intensity = 0;
        lensFlare.intensity = 0;
        lensFlare.scale = 0;
        explosionVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        explosionVFX.SetFloat("explosionDuration", explosionDuration * _power);
        explosionVFX.SendEvent("Trigger");
        
        StartCoroutine(RumbleDelay());
        yield return new WaitForFixedUpdate();
        var size = Physics.OverlapSphereNonAlloc(ExplosionPoint.position, explosionRadius * _power, _colliders);
        for (int i = 0; i < size; i++)
        {
            var hit = _colliders[i];
            if (hit.CompareTag("Destructible"))
                hit.SendMessage("Damage", 100, SendMessageOptions.DontRequireReceiver);
            else if (hit.TryGetComponent(out IHealth health))
            {
                // var distance = Vector3.Distance(transform.position, hit.transform.position)
            }
        }
        yield return new WaitForSeconds(explosionDuration * _power);
        explosionVFX.SendEvent("StopEffect"); 
        
        // return explosion point forward to have the turret go back straight
        ExplosionPoint.SetParent(transform.parent, true);
        ExplosionPoint.position = transform.position + transform.parent.forward * 100;
        InProgress = false;
    }

    public void InterruptCharge()
    {
        if (!Charging) return;
        StopCoroutine(_shootCoroutine);
        _lightTween.Kill();
        _intensityTween.Kill();
        _scaleTween.Kill();
        flareLight.intensity = 0;
        lensFlare.intensity = 0;
        lensFlare.scale = 0;
        Charging = false;
        
        beamChargingVFX.SendEvent("StopEffect");
        beamChargingVFX.Stop();
        audioSource.Stop();
    }

    private float _power;
    public void ShootBeam(Vector3 position, float power = 1f)
    {
        _power = power;
        InProgress = true;
        // detach the explosion point for it not to move (it will bug lightnings bolts otherwise)
        ExplosionPoint.position = position;
        ExplosionPoint.transform.SetParent(null, true);
        
        beamTravelVFX.SetVector3("StartPosition", transform.position);
        beamTravelVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        beamTravelVFX.SetFloat("VFX Scale", transform.localScale.x);
        beamChargingVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        beamChargingVFX.SetFloat("ChargingTime", beamChargeTime * power + wingDowntime);
        beamChargingVFX.SendEvent("Trigger"); 
        explosionVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        explosionVFX.SetFloat("explosionDuration", explosionDuration * power);
        explosionVFX.transform.localScale = _explosionScale * power;
        _shootCoroutine = StartCoroutine(ChargingBeam());
    }

    

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.yKey.wasPressedThisFrame)
        {
            ShootBeam(ExplosionPoint.position);
        }
    }
}
