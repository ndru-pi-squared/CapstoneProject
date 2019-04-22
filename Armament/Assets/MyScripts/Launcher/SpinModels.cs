using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinModels : MonoBehaviour
{
    [Tooltip("Number of rotations per second")]
    [SerializeField] private float rotationRate = .5f; // default is rotating 360 degrees in 2 seconds
    [SerializeField] private GameObject[] avatarModels; // currently only works if there are exactly two models
    // Start is called before the first frame update
    void Start()
    {
        if (avatarModels.Length != 2)
        {
            Debug.LogError("SpinModels: Start() Wrong number of avatar models specified in inspector, you dickhead!");
            return;
        }
        if (avatarModels[0] == null || avatarModels[1] == null)
        {
            Debug.LogError("SpinModels: Start() You forgot to set up the avatar models in the inspector, you shitstain!");
            return;
        }
    }

    void Update()
    {
        // Rotate the models
        avatarModels[0].transform.Rotate(0, 360 * rotationRate * Time.deltaTime, 0);
        avatarModels[1].transform.Rotate(0, 360 * rotationRate * Time.deltaTime, 0);
    }
}