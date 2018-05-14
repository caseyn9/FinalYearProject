using System.Collections;
using System.Collections.Generic;
using System;
using System.Drawing;
using UnityEngine;

public class Thumb : MonoBehaviour {

    // Use this for initialization
    Test1 hand;
    GameObject videoPlane;
    Vector2 planeBox;
    public float lengthOut = 0;
    public float heightOut = 0;
    public int[] handHW = new int[2];
    void Start () {
        videoPlane = GameObject.Find("Plane");
        hand = videoPlane.GetComponent<Test1>();
        Renderer planeRenderer = videoPlane.GetComponent<Renderer>();
        float length = planeRenderer.bounds.size.x/10;
        lengthOut = length;
        float height = planeRenderer.bounds.size.y/10;
        heightOut = height;
        float depth = planeRenderer.bounds.size.z;
        planeBox = new Vector2(length, height);
        
    }
	
	// Update is called once per frame
	void Update () {
        handHW[0] = hand.outWidth;
        handHW[1] = hand.outHeight;
        Vector2 translatePoint = PointToUnit(hand.fingerPoints[0], planeBox, handHW);
        transform.position = new Vector3(translatePoint.x , translatePoint.y+1, transform.position.z);
    }

    //convert emgu camera pixels to unity x,y points
    Vector2 PointToUnit(Point p, Vector2 planeWH, int[] cameraWH)
    {
        double widthRatio = planeWH.x / cameraWH[0];
        double heightRatio = planeWH.y / cameraWH[1];
        //Debug.Log("cameraWH :" + cameraWH[0] + "," + cameraWH[1]);
        //Debug.Log("planeWH :" + planeWH);
        //Debug.Log("widthRatio :" + widthRatio);
      //  Debug.Log("pixel point" + p);
       // Debug.Log(" unity point" + p.X * (float)widthRatio + "," + p.Y * (float)heightRatio * -1);
        Vector2 ret = new Vector2(p.X * (float)widthRatio , p.Y * (float)heightRatio *-1);
        return ret;
    }
}
