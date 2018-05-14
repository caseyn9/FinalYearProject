using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexFinger : MonoBehaviour {
    GameObject tip;
    // Use this for initialization
    void Start () {
        tip = GameObject.Find("IndexTip");
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 target = tip.transform.position;
        transform.LookAt(target);
    }
}
