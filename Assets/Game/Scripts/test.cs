using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("Awake");
    }
    void Start()
    {
        Debug.Log("Start");
    }
    // Update is called once per frame
    void Update()
    {
        Debug.Log("Update");
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy");
    }

    private void OnEnable()
    {
        Debug.Log("OnEnable");
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable");
    }

    private void OnValidate()
    {
        Debug.Log("OnValidate");
    }
}
