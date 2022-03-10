using System;
using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using UnityEngine;

public class PoseUpdater : MonoBehaviour
{

    private float lastUpdate;
    public float maximumDuration = 1f;
    public int id;

    // TODO: move duration in frames instead ? As the updates are in frames...
    public void CheckPosition()
    {
        if(Time.time - lastUpdate > maximumDuration)
        {
            Destroy(this.gameObject);
            Debug.Log("KILL " + id);
        }
    }

    public void UpdatePose(MarkerInfo markerInfo)
    {
        id = markerInfo.id;

        Vector3 m = markerInfo.rotation;
        // https://answers.opencv.org/question/110441/use-rotation-vector-from-aruco-in-unity3d/?sort=latest
        float theta = (float)(Math.Sqrt(m.x * m.x + m.y * m.y + m.z * m.z) * 180 / Math.PI);
        Vector3 axis = new Vector3(-m.x, m.y, -m.z);
        Quaternion rot = Quaternion.AngleAxis(theta, axis);
       
        // If use SolvePNP
        // Vector3 f; // from OpenCV
        // Vector3 u; // from OpenCV
        // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
        //Quaternion rot = Quaternion.LookRotation(new Vector3(f.x, -f.y, f.z), new Vector3(u.x, -u.y, u.z));
     
        // https://stackoverflow.com/questions/36561593/opencv-rotation-rodrigues-and-translation-vectors-for-positioning-3d-object-in
        // STEP 1 : fetch position from OpenCV + basic transformation
        Vector3 pos = markerInfo.translation; // from OpenCV
        pos = new Vector3(pos.x, -pos.y, pos.z); // right-handed coordinates system (OpenCV) to left-handed one (Unity)

        this.transform.localRotation = rot;
        this.transform.localPosition = pos;
        // Correct 90 degree rotation
        this.transform.Rotate(new Vector3(90, 0, 0));

        lastUpdate = Time.time;
    }
}
