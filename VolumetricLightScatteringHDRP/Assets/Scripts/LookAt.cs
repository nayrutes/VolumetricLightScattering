using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LookAt : MonoBehaviour
{
    public GameObject g;
    public GameObject lookPoint;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //this.gameObject.transform.LookAt(g.transform.position, Vector3.up);
        this.gameObject.transform.right = (lookPoint.transform.position - this.transform.position);
    }
}
