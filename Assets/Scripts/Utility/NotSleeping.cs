using System;
using System.Collections;
using UnityEngine;

namespace Utility
{
    public class NotSleeping : MonoBehaviour
    {
        [SerializeField] private GameObject stoppedMorpho;
        [SerializeField] private GameObject morphoShootPrefab;
        [SerializeField] private GameObject morphoWalkPrefab;
        [SerializeField] private MorphoObstacleChannel stopChannel;

        IEnumerator ShootMorpho()
        {
            var morpho = Instantiate(morphoShootPrefab, morphoShootPrefab.transform.position, morphoShootPrefab.transform.rotation);
            morpho.gameObject.SetActive(true);
            Destroy(morpho, 40);
            yield return new WaitForSeconds(1);
            morpho.transform.rotation = stoppedMorpho.transform.rotation;
            stopChannel.SendEventMessage(stoppedMorpho.transform);
            yield return null;
        }
        
        IEnumerator WalkMorpho()
        {
            var morpho = Instantiate(morphoWalkPrefab, morphoWalkPrefab.transform.position, morphoWalkPrefab.transform.rotation);
            morpho.gameObject.SetActive(true);
            Destroy(morpho, 180);
            yield return null;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                StartCoroutine(ShootMorpho());
            }
            
            if (Input.GetKeyDown(KeyCode.L))
            {
                StartCoroutine(WalkMorpho());
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                stopChannel.SendEventMessage(transform);
            }
        }
    }
}