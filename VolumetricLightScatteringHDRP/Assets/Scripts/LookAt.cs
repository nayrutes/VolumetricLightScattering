using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class LookAt : MonoBehaviour
{
    public GameObject g;
    public GameObject lookPoint;

    public bool lookParallel = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (lookPoint == null)
            return;
        //this.gameObject.transform.LookAt(g.transform.position, Vector3.up);
        if (!lookParallel)
        {
            this.gameObject.transform.right = (lookPoint.transform.position - this.transform.position);
        }
        else
        {
            this.gameObject.transform.right = lookPoint.transform.right;
        }
    }
}
