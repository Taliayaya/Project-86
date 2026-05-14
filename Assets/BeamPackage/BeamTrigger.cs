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
    

    private void Start()
    {
        explosionVFX.transform.SetParent(null, true);
        explosionVFX.transform.rotation = Quaternion.identity;
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

    IEnumerator ChargingBeam()
    {
        SetAnimatorTrigger("PrepareShoot");
        ChargingAudio();
        DOTween.To(() => lensFlare.scale, v => lensFlare.scale = v, flareChargingScale, flareTransitionDuration);

        DOTween.To(() => lensFlare.intensity, v => lensFlare.intensity = v, flareChargingIntensity, flareTransitionDuration);
        DOTween.To(() => flareLight.intensity, v => flareLight.intensity = v, flareChargingIntensity, flareTransitionDuration);

        
        yield return new WaitForSeconds(beamChargeTime);
        
        SetAnimatorTrigger("Shoot");
        DOTween.To(() => lensFlare.scale, v => lensFlare.scale = v, flarePrepareShootScale, flarePrepareTransitionDuration);
        DOTween.To(() => lensFlare.intensity, v => lensFlare.intensity = v, flarePrepareShootIntensity, flarePrepareTransitionDuration);
        DOTween.To(() => flareLight.intensity, v => flareLight.intensity = v, flareLightPrepareShootIntensity, flarePrepareTransitionDuration);

        yield return new WaitForSeconds(wingDowntime);
        beamTravelVFX.SetVector3("StartPosition", transform.position);
        beamTravelVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        beamTravelVFX.SendEvent("Trigger");
        beamChargingVFX.SendEvent("StopEffect");
        audioSource.Stop();
        yield return  new WaitForSeconds(beamTravelTime);
        flareLight.intensity = 0;
        lensFlare.intensity = 0;
        lensFlare.scale = 0;
        explosionVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        explosionVFX.SetFloat("explosionDuration", explosionDuration);
        explosionVFX.SendEvent("Trigger");
        StartCoroutine(RumbleDelay());
        yield return new WaitForSeconds(explosionDuration);
        explosionVFX.SendEvent("StopEffect"); 
        
        // return explosion point forward to have the turret go back straight
        ExplosionPoint.SetParent(transform.parent, true);
        ExplosionPoint.position = transform.position + transform.parent.forward * 100;
    }

    public void ShootBeam(Vector3 position)
    {
        // detach the explosion point for it not to move (it will bug lightnings bolts otherwise)
        ExplosionPoint.position = position;
        ExplosionPoint.transform.SetParent(null, true);
        
        beamTravelVFX.SetVector3("StartPosition", transform.position);
        beamTravelVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        beamTravelVFX.SetFloat("VFX Scale", transform.localScale.x);
        beamChargingVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        beamChargingVFX.SetFloat("ChargingTime", beamChargeTime + wingDowntime);
        beamChargingVFX.SendEvent("Trigger"); 
        explosionVFX.SetVector3("ExplosionPosition", ExplosionPoint.position);
        explosionVFX.SetFloat("explosionDuration", explosionDuration);
        StartCoroutine(ChargingBeam());
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
