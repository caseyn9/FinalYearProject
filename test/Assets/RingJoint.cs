using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingJoint : MonoBehaviour {

    GameObject tip;
    GameObject palm;
    void Start()
    {
        tip = GameObject.Find("RingTip");
        palm = GameObject.Find("Palm");
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target = tip.transform.position;
        transform.LookAt(target);
    }
}
