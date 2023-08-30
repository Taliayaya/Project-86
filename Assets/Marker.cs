using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Marker : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 5, 0);
    [SerializeField] private TMP_Text distanceText;

    private void Update()
    {
        Vector2 pos = Camera.main.WorldToScreenPoint(target.position + offset);
        icon.transform.position = pos;
        if (PlayerManager.Player == null) return;

        if (Vector3.Angle(target.position - PlayerManager.PlayerPosition, PlayerManager.Player.transform.forward) > 90)
        {
            // It is behind us
            icon.gameObject.SetActive(false);
        }
        else
        {
            // In front of us
            icon.gameObject.SetActive(true);
            var distance = Vector3.Distance(target.position, PlayerManager.PlayerPosition);
            distanceText.text = (distance > 1000 ? $"{(distance / 1000):F1}km" : $"{distance:F0}m");
        }
    }
}
