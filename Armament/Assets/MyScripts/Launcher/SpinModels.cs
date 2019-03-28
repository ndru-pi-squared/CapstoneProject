using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinModels : MonoBehaviour
{
    [SerializeField] private GameObject kyleRobotModel;
    [SerializeField] private GameObject unityChanModel;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        kyleRobotModel.transform.Rotate(0, 1, 0);
        unityChanModel.transform.Rotate(0, 1, 0);
    }
}