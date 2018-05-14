using System;
using System.IO;
using Emgu.CV; // Contains Mat and CvInvoke classes
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using UnityEngine;

public class Test1 : MonoBehaviour
{
    bool accuratePalm = false;
    public Point[] fingerPoints; 
    public int outWidth = 0;
    public int outHeight = 0;
    public Point palmCenter;
    VideoCapture cap;
    Renderer rend;
    MCvScalar skinHsv = new MCvScalar(20, 120, 120); //default skin color
    // Use this for initialization
    void Start()
    {
        palmCenter = new Point();
        fingerPoints = new Point[6] { new Point(0, 0), new Point(0, 0), new Point(0, 0), new Point(0, 0), new Point(0, 0), new Point(0, 0) }; //0 is thumb, 4 is pinky.
        rend = GetComponent<Renderer>();
        cap = new VideoCapture(0);
        //outputThumbPoint = new Point(0, 0);
        //fill plane with video stream
        Camera cam = Camera.main;
        float pos = (cam.nearClipPlane + 100f);
        transform.position = cam.transform.position + cam.transform.forward * pos;
        transform.LookAt(cam.transform);
        transform.Rotate(90.0f, 0.0f, 0.0f); 
        float h = (Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * pos * 2f) / 10.0f;
        transform.localScale = new Vector3(h * cam.aspect, 1.0f, h);

        Mat frame = cap.QueryFrame();
        outWidth = frame.Cols;
        outHeight = frame.Rows;
    }

    // Update is called once per frame
    void Update()
    {
        //fill the camera

        if (cap != null && cap.IsOpened)
        {
            Mat frame = cap.QueryFrame();
            Mat copy = frame.Clone();
            Image<Bgr, byte> image = new Image<Bgr, byte>(frame.Bitmap);
            Image<Bgr, byte> originalImage = new Image<Bgr, byte>(copy.Bitmap);
            image = ProcessFrame(frame, skinHsv);
            Point readPoint = new Point(300, 300);
            image.Draw(new CircleF(new PointF(readPoint.X, readPoint.Y), 5), new Bgr(255, 255, 100));
            originalImage.Draw(new CircleF(new PointF(readPoint.X, readPoint.Y), 5), new Bgr(255, 255, 100));
            if (Input.GetKeyDown("space"))
            {
                skinHsv = GetHsvAt(frame, readPoint);
            }

            //CvInvoke.Imshow("debug", image);
            //stream video to plane.
            Texture2D tex = new Texture2D(640, 480);
            MemoryStream memstream = new MemoryStream();
            originalImage.Bitmap.Save(memstream, originalImage.Bitmap.RawFormat);
            tex.LoadImage(memstream.ToArray());
            rend.material.mainTexture = tex;
            Debug.Log("Palm center inside test1 " + palmCenter);
            tex = null;
            frame.Dispose();
            image.Dispose();
            memstream.Dispose();
            Resources.UnloadUnusedAssets();
        }
        //restart camera in the event webcam stops unexpectedly
        else
        {
            cap = new VideoCapture();
        }
    }

    Image<Bgr, byte> ProcessFrame(Mat colorPicture, MCvScalar skinHsv)
    {//, Mat binPicture) {
        Mat picture = colorPicture.Clone();
        picture = BackgroundSubtraction(picture, skinHsv);
        //picture = binPicture;
        //return new Image<Bgr, byte>(picture.Bitmap);

        //contour stuff
        VectorOfVectorOfPoint contoursss = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(picture, contoursss, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
        VectorOfPoint handContour = FindLargestContour(contoursss);
        if ((handContour == null || CvInvoke.ContourArea(handContour) < 100 || CvInvoke.ContourArea(handContour) > 200000))
        {
            return new Image<Bgr, byte>(colorPicture.Bitmap);
        }

        VectorOfVectorOfPoint hulls = new VectorOfVectorOfPoint(1);
        //VectorOfVectorOfPoint hullDefects = new VectorOfVectorOfPoint(1);
        VectorOfInt hullI = new VectorOfInt();
        CvInvoke.ConvexHull(handContour, hullI, false, false);
        CvInvoke.ConvexHull(handContour, hulls[0], false, true);

        //convexity defects
        Mat defects = new Mat();
        CvInvoke.ConvexityDefects(handContour, hullI, defects);
        try
        {
            Matrix<int> m = new Matrix<int>(defects.Rows, defects.Cols, defects.NumberOfChannels); // copy Mat to a matrix...
            defects.CopyTo(m);
            CvInvoke.DrawContours(colorPicture, hulls, -1, new MCvScalar(0, 0, 255), 1);
            Image<Bgr, byte> image = new Image<Bgr, byte>(colorPicture.Bitmap);
            return DrawPoints(image, m, handContour);
        }
        catch (Exception)
        {
            return new Image<Bgr, byte>(colorPicture.Bitmap);
        }

        //CvInvoke.Imshow("picture", colorPicture);
        //CvInvoke.WaitKey(); // Render image and keep window opened until any key is pressed
    }

    VectorOfPoint FindLargestContour(VectorOfVectorOfPoint contours)
    {
        try
        {
            if (contours.Size > 0)
            {
                VectorOfPoint largest = contours[0];
                for (int i = 1; i < contours.Size; i++)
                {
                    if (CvInvoke.ContourArea(contours[i]) > CvInvoke.ContourArea(largest))
                    {
                        largest = contours[i];
                    }
                }
                return largest;
            }
            else
            {
                return null;
            }
        }
        catch (Exception e)
        {
            return null;
        }
    }
    Image<Bgr, byte> DrawPoints(Image<Bgr, byte> image, Matrix<int> defects, VectorOfPoint contour)
    {
        Matrix<int>[] channels = defects.Split();
        Bgr green = new Bgr(100, 255, 100);
        Bgr red = new Bgr(100, 100, 255);
        Bgr blue = new Bgr(255, 100, 100);
        VectorOfPoint fingerTips = new VectorOfPoint();
        VectorOfPoint palmPoints = new VectorOfPoint();     //palmPoints are a vector of all the defects

        //populate vector of all defects
        for (int i = 0; i < defects.Rows; i++)
        {
            palmPoints.Push(new Point[] { contour[channels[1][i, 0]] });
        }

        //find center of contour
        MCvMoments moments = CvInvoke.Moments(contour);
        double cx = moments.M10 / moments.M00;
        double cy = moments.M01 / moments.M00;
        PointF centerPoint = new PointF((float)cx, (float)cy);
        palmCenter.X = (int)cx - outWidth / 2;
        palmCenter.Y = (int)cy - outHeight / 2;
        fingerPoints[5] = palmCenter;
        image.Draw(new CircleF(centerPoint, 5), green, 5);
        //using center of contour, find min enclosed circle around an area
        //CircleF palmCirlce = FindPalm(contour, new Point((int)centerPoint.X, (int)centerPoint.Y), 50);
        //image.Draw(palmCirlce, red, 5);

        if (accuratePalm)
        {
            CircleF palmCirlce = FindPalmAccurate(contour);
            image.Draw(palmCirlce, red, 5);
            image.Draw(new CircleF(palmCirlce.Center, 5), blue, 5);
            return image;
        }

        double greatestAngle = 0;   //greatest angle to center of hand will be thumb
        Point thumbPoint = new Point(0, 0);
        VectorOfPoint defectPoints = new VectorOfPoint();
        VectorOfPoint leftTips = new VectorOfPoint();
        VectorOfPoint rightTips = new VectorOfPoint();
        for (int i = 0; i < defects.Rows; i++)
        {
            image.Draw(new Point[] { contour[channels[0][i, 0]],        //[0] is right point
                                         contour[channels[1][i,0]],         //[1] is left point
                                         contour[channels[2][i,0]]          //[2] is midpoint, the defect
                                       },
                        green, 2);
            Point p = new Point[] { contour[channels[1][i, 0]] }[0];    //p is left
            Point p2 = new Point[] { contour[channels[0][i, 0]] }[0];   //p2 is right
            Point p3 = new Point[] { contour[channels[2][i, 0]] }[0];   //p3 is mid

            PointF[] pf = new PointF[] { new PointF(p.X, p.Y) };
            CircleF circle = new CircleF(pf[0], 5);
            PointF[] pf2 = new PointF[] { new PointF(p2.X, p2.Y) };
            CircleF circle2 = new CircleF(pf2[0], 5);
            PointF[] pf3 = new PointF[] { new PointF(p3.X, p3.Y) };
            CircleF circle3 = new CircleF(pf3[0], 5);

            double[] angle = new double[1];
            angle[0] = -1;
            if (IsFingertip(contour, channels, i, angle))
            {
                leftTips.Push(new Point[] { p });
                rightTips.Push(new Point[] { p2 });
                defectPoints.Push(new Point[] { p3 });
                // image.Draw(circle, red, 5);
                // image.Draw(circle2, blue, 5);
                //CvInvoke.PutText(image, angle[0].ToString(), p, FontFace.HersheyPlain, 1, new MCvScalar(255,255,255), 2);
                image.Draw(circle3, blue, 5);

                //check if thumb
                double thumbAngle = AngleOf(p, p2, centerPoint);
                if (thumbAngle > greatestAngle && thumbAngle > 35)  //35 found though experimentation to be lowest thumb angle
                {
                    double distLM = Math.Sqrt((p.X - p3.X) * (p.X - p3.X) + (p.Y - p3.Y) * (p.Y - p3.Y));
                    double distRM = Math.Sqrt((p2.X - p3.X) * (p2.X - p3.X) + (p2.Y - p3.Y) * (p2.Y - p3.Y));
                    greatestAngle = angle[0];
                    //thumb should be smallest distance to the midpoint
                    if (distLM < distRM)
                    {
                        thumbPoint = p;
                    }
                    else
                    {
                        thumbPoint = p2;
                    }
                }
                fingerTips.Push(new Point[] { p });
            }
        }

        //find close by tips, choose left one. The three middle tips are each counted twice
        VectorOfPoint finalTips = new VectorOfPoint();
        if (leftTips.Size != 0 && rightTips.Size != 0)
        {
            finalTips = FinalizeTips(leftTips, rightTips);
        }
        //draw tips
        int thumbIndex = 0;
        for (int i = 0; i < finalTips.Size; i++)
        {
            Point tip = finalTips[i];
            PointF tipF = tip;
            CircleF tipCircle = new CircleF(tipF, 5);
            image.Draw(tipCircle, red, 5);
            if (CalculateDistance(tip, thumbPoint) < 20)
            {
                thumbIndex = i;
                Console.WriteLine("Found Thumb at index: " + i);
            }
        }
        double[] distances = new double[finalTips.Size];
        //get relative distance between thumb and all fingers
        for (int i = 0; i < finalTips.Size; i++)
        {
            Console.WriteLine("i:" + i + "\n thumb index:" + thumbIndex);
            distances[i] = CalculateDistance(finalTips[thumbIndex], finalTips[i]);
        }
        //label all fingers based on distance to thumb
        for (int i = 0; i < distances.Length; i++)
        {
            double smallest = 10000;
            int j2 = -1;
            //loop to find smallest distance
            for (int j = 0; j < distances.Length; j++)
            {
                if (distances[j] < smallest)
                {
                    smallest = distances[j];
                    j2 = j;
                }
            }
            distances[j2] = 200000;  //make distance longer so we can see next shortest distance on next pass
                                     //then switch on index of said distance;
            switch (i)
            {
                case 0:
                    CvInvoke.PutText(image, "Thumb", finalTips[j2], FontFace.HersheyPlain, 1, new MCvScalar(0, 0, 0), 2);
                    fingerPoints[0] = new Point(finalTips[j2].X - outWidth/2, finalTips[j2].Y - outHeight/2);
                    Debug.Log("thumb :" + fingerPoints[0]);

                    break;
                case 1:
                    CvInvoke.PutText(image, "Index", finalTips[j2], FontFace.HersheyPlain, 1, new MCvScalar(100, 100, 200), 2);
                    fingerPoints[1] = new Point(finalTips[j2].X - outWidth / 2, finalTips[j2].Y - outHeight / 2);
                    break;
                case 2:
                    CvInvoke.PutText(image, "Middle", finalTips[j2], FontFace.HersheyPlain, 1, new MCvScalar(100, 200, 100), 2);
                    fingerPoints[2] = new Point(finalTips[j2].X - outWidth / 2, finalTips[j2].Y - outHeight / 2);
                    break;
                case 3:
                    CvInvoke.PutText(image, "Ring", finalTips[j2], FontFace.HersheyPlain, 1, new MCvScalar(200, 100, 100), 2);
                    fingerPoints[3] = new Point(finalTips[j2].X - outWidth / 2, finalTips[j2].Y - outHeight / 2);
                    break;
                case 4:
                    CvInvoke.PutText(image, "Pinky", finalTips[j2], FontFace.HersheyPlain, 1, new MCvScalar(200, 200, 200), 2);
                    fingerPoints[4] = new Point(finalTips[j2].X - outWidth / 2, finalTips[j2].Y - outHeight / 2);
                    break;
            }
        }

        //CvInvoke.PutText(image, "Thumb", thumbPoint, FontFace.HersheyPlain, 1, new MCvScalar(0, 0, 0), 2);
        if (defectPoints.Size != 0)
        {
            //CircleF palm = CvInvoke.MinEnclosingCircle(defectPoints);
            //image.Draw(palm, red, 1);
        }
        return image;
    }

    VectorOfPoint FinalizeTips(VectorOfPoint leftTips, VectorOfPoint rightTips)
    {
        VectorOfPoint finalTips = new VectorOfPoint();
        for (int i = 0; i < leftTips.Size; i++)
        {
            int j2 = -1;
            double smallestDist = 10000;    //high default value
            Point a = leftTips[i];
            for (int j = 0; j < rightTips.Size; j++)
            {
                Point b = rightTips[j];
                double dist = CalculateDistance(a, b);
                if (dist < smallestDist)
                {
                    smallestDist = dist;
                    j2 = j;
                }
            }
            Console.WriteLine("leftTip num" + leftTips.Size);
            Console.WriteLine("rightTip num" + rightTips.Size);

            Console.WriteLine("dist" + smallestDist);
            finalTips.Push(new Point[] { a });
            if (smallestDist < 25)
            {
                //get rid of duplicate tip from rightTips
                VectorOfPoint tmp = new VectorOfPoint();
                for (int j = 0; j < rightTips.Size; j++)
                {
                    if (j != j2)
                    {
                        tmp.Push(new Point[] { rightTips[j] });
                    }
                }
                rightTips = tmp;
            }
        }
        //then add the rest of right tips (hopefully just one left)
        for (int i = 0; i < rightTips.Size; i++)
        {
            Point c = rightTips[i];
            finalTips.Push(new Point[] { c });
        }
        return finalTips;
    }
    double CalculateDistance(Point a, Point b)
    {
        return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
    //checks whether point in given matrix can be regarded as fingerprints.
    bool IsFingertip(VectorOfPoint contour, Matrix<int>[] channels, int index, double[] angleReturn)
    {
        //points[0] = right tip, points[1] =left tip, points[2] = midpoint
        Point[] points = new Point[]{ contour[channels[0][index, 0]],
                                          contour[channels[1][index,0]],
                                          contour[channels[2][index,0]]
                                        };
        //check angle between right tip, left tip and midpoint.
        PointF rightTip = new PointF(points[0].X, points[0].Y);
        PointF leftTip = new PointF(points[1].X, points[1].Y);
        PointF midPoint = new PointF(points[2].X, points[2].Y);
        double angle = AngleOf(rightTip, leftTip, midPoint);
        if (angle > 20 && angle < 90)
        {
            //get center of contour(hand)
            MCvMoments moments = CvInvoke.Moments(contour);
            double cx = moments.M10 / moments.M00;
            double cy = moments.M01 / moments.M00;
            PointF centerPoint = new PointF((float)cx, (float)cy);
            angle = AngleOf(rightTip, leftTip, centerPoint);
            if (angle > 0 && angle < 90)
            {
                //check distance of left tip to midpoint
                double dist = Math.Sqrt((leftTip.X - midPoint.X) * (leftTip.X - midPoint.X) + (leftTip.Y - midPoint.Y) * (leftTip.Y - midPoint.Y));
                if (dist > 10 && dist < 250)
                {
                    //check dist of midpoints to center of contour
                    dist = Math.Sqrt((midPoint.X - centerPoint.X) * (midPoint.X - centerPoint.X) + (midPoint.Y - centerPoint.Y) * (midPoint.Y - centerPoint.Y));
                    if (dist < 150)
                    {
                        angleReturn[0] = angle;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    //will break when trying to access point outside frame i think
    CircleF FindPalm(VectorOfPoint contour, Point searchAround, int searchArea)
    {
        Point[] searchPoints = new Point[searchArea * searchArea];
        Point topLeft = new Point(searchAround.X - searchArea, searchAround.Y - searchArea);
        Point centerPoint = new Point();
        double shortestDist = 9999999;
        for (int i = 0; i < searchPoints.Length; i++)
        {
            for (int j = 0; j < searchArea; j++)
            {
                Point tmp = new Point(topLeft.X + i, topLeft.Y + j);
                for (int k = 0; k < contour.Size; k++)
                {
                    double dist = CalculateDistance(tmp, contour[k]);
                    if (dist < shortestDist)
                    {
                        shortestDist = dist;
                        centerPoint = tmp;
                    }
                }
            }
        }
        return new CircleF(centerPoint, (float)  shortestDist);
    }
    //finds inner angle between 3 points, inner angle point is final paremeter.
    //subtracts both points by the middle point of the defect region, ten calculates arc cosine of inner product divided by the norm of the vectors.
    double AngleOf(PointF right, PointF left, PointF mid)
    {
        double angle = -1;
        double distLM = Math.Sqrt((left.X - mid.X) * (left.X - mid.X) + (left.Y - mid.Y) * (left.Y - mid.Y));
        double distRM = Math.Sqrt((right.X - mid.X) * (right.X - mid.X) + (right.Y - mid.Y) * (right.Y - mid.Y));
        double distRL = Math.Sqrt((right.X - left.X) * (right.X - left.X) + (right.Y - left.Y) * (right.Y - left.Y));
        angle = Math.Acos(((distRM * distRM) + (distLM * distLM) - (distRL * distRL)) / (2 * distLM * distRM));
        angle = angle * 180 / Math.PI;  //convert from radians to degrees.
        return angle;
    }
    Mat BackgroundSubtraction(Mat img, MCvScalar skinHsv)
    {
        //convert to hsv
        CvInvoke.CvtColor(img, img, ColorConversion.Bgr2Hsv);
        ScalarArray skin = new ScalarArray(skinHsv);
        ScalarArray skin2 = new ScalarArray(new MCvScalar(skinHsv.V0 + 4, skinHsv.V1 + 20, skinHsv.V2 + 20));
        ScalarArray scalarL = new ScalarArray(new MCvScalar(skinHsv.V0 - 20, skinHsv.V1 - 80, skinHsv.V2- 80));// skinHsv.V2 - 60));    //lower skin hsv range skinHsv.V2 - 120)
        ScalarArray scalarU = new ScalarArray(new MCvScalar(skinHsv.V0 + 20, skinHsv.V1 + 80, skinHsv.V2 + 255));// skinHsv.V2 + 60)); //upper skin hsv range skinHsv.V2 + 120
                                                                                                    //CvInvoke.InRange(img, skin, skin2, img);
        CvInvoke.InRange(img, scalarL, scalarU, img);
        Mat stucturingElement = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(5, 5), new Point(-1, -1));    //Point(-1,-1) means it is anchored at center

        //CvInvoke.Imshow("foreground before open,close", img);
        //open and close
        CvInvoke.Erode(img, img, stucturingElement, new Point(-1, -1), 1, BorderType.Constant, default(MCvScalar));
        CvInvoke.Dilate(img, img, stucturingElement, new Point(-1, -1), 1, BorderType.Constant, default(MCvScalar));

        CvInvoke.Dilate(img, img, stucturingElement, new Point(-1, -1), 1, BorderType.Constant, default(MCvScalar));
        CvInvoke.Erode(img, img, stucturingElement, new Point(-1, -1), 1, BorderType.Constant, default(MCvScalar));

        CvInvoke.GaussianBlur(img, img, new Size(9, 9), 0);
        //CvInvoke.Imshow("foreground", img);
        CvInvoke.WaitKey(30);
        return img;

    }

     MCvScalar GetHsvAt(Mat img, Point p)
    {
        Image<Hsv, byte> hsvImg = new Image<Hsv, byte>(img.Bitmap);
        Hsv color = hsvImg[p.X, p.Y];
        return new MCvScalar(color.Hue, color.Satuation, color.Value);
    }

    static CircleF FindPalmAccurate(VectorOfPoint contour)
    {
        double timeTook = 0;
        var CurDate = DateTime.Now;
        double dist = -2;
        double maxDist = -1;
        Point center = new Point();
        for (int i = 0; i < 600; i++)
        {
            for (int j = 0; j < 400; j++)
            {

                dist = CvInvoke.PointPolygonTest(contour, new Point(i, j), true);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    center = new Point(i, j);
                }
            }
        }
        var CurDate2 = DateTime.Now;
        Console.WriteLine("Time took to find palm: " + (CurDate2.Second - CurDate.Second) + "." + (CurDate2.Millisecond - CurDate.Millisecond));
        return new CircleF(center, (float)maxDist);
    }

}


