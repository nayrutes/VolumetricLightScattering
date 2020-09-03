using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField] private GameObject go;

    [SerializeField] private float speed = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        go.transform.Rotate(Vector3.forward,speed*Time.deltaTime);
    }
}
