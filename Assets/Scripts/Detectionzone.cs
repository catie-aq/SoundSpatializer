using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionZone : MonoBehaviour
{
    private CarDetectionZone carDetectionZone;
    private BoxCollider boxCollider;
    public string ZoneName;

    private float zoneWidth = 4.5f;  // meters
    private float zoneSecondWidth = 12.0f;  // meters
    private float centralLength = 12.0f; // meters
    private float carLength = 4.0f; // meters
    private Vector3 coordinates;
    private float defaultSpeed = 30f;

    private void OnTriggerEnter(Collider other)
    {
        CheckZoneManager();
        if (other.tag == "Vehicle")
        {
            
            carDetectionZone.EnteringCollision(ZoneName, other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CheckZoneManager();
        if (other.tag == "Vehicle")
        {
            carDetectionZone.LeavingCollision(ZoneName, other.gameObject);
        }
    }

    private void CheckZoneManager()
    {
        if (carDetectionZone == null)
        {
            Debug.LogError("Zone manager not set.");
        }
    }

    internal void Initialize(CarDetectionZone carDetectionZone, string name, Transform transform, Vector3 coord)
    {
        this.coordinates = coord;
        
        this.carDetectionZone = carDetectionZone;
        this.transform.SetParent(transform);
        this.tag = "Zone";
        this.ZoneName = name;

        // Reset position - else it will use the parent’s transform.
        this.transform.localPosition = new Vector3(0, 0, 0);
        BoxCollider boxC = this.gameObject.AddComponent<BoxCollider>();
        boxC.isTrigger = true;
        this.boxCollider = boxC;

        UpdateColliderPositionAndSize(defaultSpeed);

        // The RigidBody enbables the collisions
        Rigidbody rigidB = this.gameObject.AddComponent<Rigidbody>();
        rigidB.isKinematic = true;
    }

    public void UpdateColliderPositionAndSize(float speed)
    {
        speed = Math.Abs(speed);
        float x = coordinates.y;
        float z = coordinates.x * zoneWidth;

        float detectionWidth = zoneWidth / 2f;

        // Patch to increse the detection size of the sides
        if (Math.Abs(coordinates.x) == 2)
        {
           detectionWidth = zoneSecondWidth / 2f;
        }
        
        // Fixed size is 6 meters, variable with speed. 
        if (coordinates.y == 0)
        {
            boxCollider.center = new Vector3(0, 1, z);
            boxCollider.size = new Vector3(centralLength, 1, detectionWidth);
            return;
        }

        float l0 = centralLength;
        float l1 = l0 + (speed * 0.6f);
        float l2 = l1 * 3;

        float p1 =      l0 / 2 + l1/2;
        float p2 = p1 + l1 / 2 + l2/2;

        // Length is diminshed by size of a car to avoid double detection 

        // Fixed size is 6 meters, variable with speed. 
        if (Math.Abs(coordinates.y) == 1)
        {
            boxCollider.center = new Vector3(p1 * coordinates.y , 1, z);
            boxCollider.size = new Vector3(l1 - carLength, 1, detectionWidth);
            return;
        }

        if (Math.Abs(coordinates.y) == 2)
        {        
            boxCollider.center = new Vector3(p2 * coordinates.y/2, 1, z);
            boxCollider.size = new Vector3(l2 - carLength, 1, detectionWidth);
            return;
        }
        
    }
}
