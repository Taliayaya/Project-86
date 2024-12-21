using System.Collections;
using TMPro;
using UnityEngine;

public class ShowAfterDelay : MonoBehaviour
{
    public float delayInSeconds = 3f; // Set the delay time in seconds
    public MonoBehaviour monoBehaviour;

    private void Start()
    {
        // Disable the TextMeshPro component initially
        monoBehaviour.enabled = false;

        // Start the coroutine to show the text after the delay
        StartCoroutine(ShowTextDelayed());
    }

    private IEnumerator ShowTextDelayed()
    {
        // Wait for the specified delay time
        yield return new WaitForSeconds(delayInSeconds);

        // Enable or show the TextMeshPro component after the delay
        monoBehaviour.enabled = true;
    }
}
