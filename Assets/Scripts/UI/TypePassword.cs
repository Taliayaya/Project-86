using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;

public class TypePassword : MonoBehaviour
{
    [SerializeField] private float timeToType = 1.5f;
    [SerializeField] private int maxCharacters = 10;

    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject[] screensCanvas;
    private TMP_InputField _inputField;
    // Start is called before the first frame update
    void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator Type()
    {
        for (int i = 0; i < maxCharacters; ++i)
        {
            _inputField.text += Random.Range(0, 10);
            yield return new WaitForSeconds(timeToType/maxCharacters);
        }

        yield return new WaitForSeconds(1.5f);
        virtualCamera.gameObject.SetActive(false);
        loginCanvas.SetActive(false);
        foreach (var screen in screensCanvas)
        {
            screen.SetActive(true);
        }
    }

    public void StartType()
    {
        StartCoroutine(Type());
    }
}
