using System;
using UnityEngine;
using System.Drawing;

public class Palm : MonoBehaviour {
    // Use this for initialization
    Test1 hand;
    GameObject videoPlane;
    Vector2 planeBox;
    GameObject middleTip;
    float y = 3;
    float z = 3;
    float x = 1;
    float baseDist = (float)4.5;
 
    void Start()
    {
        middleTip = GameObject.Find("MiddleTip");
        videoPlane = GameObject.Find("Plane");
        //thumb = GameObject.Find("ThumbTip");
        //thumbComponent = thumb.GetComponent<Thumb>();

        hand = videoPlane.GetComponent<Test1>();
        Renderer planeRenderer = videoPlane.GetComponent<Renderer>();
        float length = planeRenderer.bounds.size.x / 10;
        float height = planeRenderer.bounds.size.y / 10;
        //length = thumbComponent.lengthOut;
        //height = thumbComponent.heightOut;
        float depth = planeRenderer.bounds.size.z;
        planeBox = new Vector2(length, height);

    }

    // Update is called once per frame
    void Update()
    {
        
        Vector2 translatePoint = PointToUnit(hand.fingerPoints[5], planeBox, new int[] { hand.outWidth, hand.outHeight });
        Vector2 midTipPoint = PointToUnit(hand.fingerPoints[2], planeBox, new int[] { hand.outWidth, hand.outHeight });
        transform.position = new Vector3(translatePoint.x, translatePoint.y + 1, transform.position.z);
        Vector3 target = middleTip.transform.position;
        double dist = CalculateDistance(translatePoint, midTipPoint);
        Debug.Log("distance: " + dist);
       // target.z = transform.position.z;
        transform.LookAt(target);
        /*
        float distDiff = (float)dist - baseDist;
        float scaleFactor = distDiff / baseDist ;
        float scaledY = transform.position.y * scaleFactor;
        float scaledZ = transform.position.z * scaleFactor;
        transform.localScale += new Vector3(0, scaledY, scaledZ);
        baseDist = (float)dist;
        */
    }

    //convert emgu camera pixels to unity x,y points
    Vector2 PointToUnit(Point p, Vector2 planeWH, int[] cameraWH)
    {
        double widthRatio = planeWH.x / cameraWH[0];
        double heightRatio = planeWH.y / cameraWH[1];
        //Debug.Log("cameraWH :" + cameraWH[0] + "," + cameraWH[1]);
        //Debug.Log("planeWH :" + planeWH);
        //Debug.Log("widthRatio :" + widthRatio);
        // Debug.Log("pixel point" + p);
        // Debug.Log(" unity point" + p.X * (float)widthRatio + "," + p.Y * (float)heightRatio * -1);
        Vector2 ret = new Vector2(p.X * (float)widthRatio, p.Y * (float)heightRatio * -1);
        return ret;
    }
    double CalculateDistance(Vector2 a, Vector2 b)
    {
        return Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }
}
