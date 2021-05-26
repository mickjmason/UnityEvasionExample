using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var lModifiedDirection = Quaternion.AngleAxis(-60, Vector3.up) * transform.forward;
        Debug.DrawLine(transform.position, transform.position + (lModifiedDirection * 6), Color.red);
        var uModifiedDirection = Quaternion.AngleAxis(60, Vector3.up) * transform.forward;
        Debug.DrawLine(transform.position, transform.position + (uModifiedDirection * 6), Color.red);
    }


}
