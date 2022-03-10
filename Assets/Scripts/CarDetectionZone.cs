using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CarDetectionZone : MonoBehaviour
{
    private GameObject[] zones = new GameObject[25];
    public GameObject CarModel;
    public RedisConnection connection;

    public float speed = 50f;
    public bool activeDrawGizmos;

    private const string ZonesKey = "zones";
    private float scaleX;
    private string[,] zoneName = new string[5, 5]  {{"E5", "E4", "E3", "E2", "E1"},
                                                    {"D5", "D4", "D3", "D2", "D1"},
                                                    {"C5", "C4", "C3", "C2", "C1"},
                                                    {"B5", "B4", "B3", "B2", "B1"},
                                                    {"A5", "A4", "A3", "A2", "A1"}};

    private List<DetectionZone> detectionZones = new List<DetectionZone>();
    // Zones names to List of game objects (other cars).
    public Dictionary<string, HashSet<GameObject>> currentCollisions = new Dictionary<string, HashSet<GameObject>>();

    private float LastSendTime = 0;
    private float SendInterval = 0.1f; // 100ms

    // Start is called before the first frame update
    void Start()
    {
        InitZoneColliders();
    }

    private void InitZoneColliders()
    {
        int index = 0;
        for (int i = 1; i < 6; i++)
        {
            for (int j = 1; j < 6; j++)
            {
                Vector2 coord = new Vector2Int(i-3, j-3);

                string name = zoneName[i - 1, j - 1];
                // Initialize the collision HashSet.
                currentCollisions[name] = new HashSet<GameObject>();
                GameObject zone = new GameObject(name);     
                var detectionZone = zone.AddComponent<DetectionZone>();
                detectionZone.Initialize(this, name, CarModel.transform, coord);
                detectionZones.Add(detectionZone);
                index++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Update each zones according to current speed.
        foreach (DetectionZone detectionZone in detectionZones)
        {
            detectionZone.UpdateColliderPositionAndSize(speed);
        }
        if(Time.time > LastSendTime + SendInterval)
        {
            PublishZoneContents();
            LastSendTime = Time.time;
        }
       
    }

    internal void EnteringCollision(string zoneName, GameObject gameObject)
    {

        // Clean the previous collisions as the object appears only once in the radar.
        // TODO: Find why some objects dissapear but should be here. 
        foreach(HashSet<GameObject> collisions in currentCollisions.Values)
        {
            if (collisions.Contains(gameObject))
            {
                collisions.Remove(gameObject);
            }
        }
        
        currentCollisions[zoneName].Add(gameObject);
        // Uncomment for debug information on collisions
        // PrintZoneContents();
        PublishZoneContents();
    }

    internal void LeavingCollision(string zoneName, GameObject gameObject)
    {
        currentCollisions[zoneName].Remove(gameObject);
        // Uncomment for debug information on collisions
        // PrintZoneContents();
        PublishZoneContents();
    }

    private void PrintZoneContents()
    {
        Vector2 mainCarSpeed = GetComponent<SpeedMonitor>().speed;
        for (int i = 1; i < 6; i++)
        {
            for (int j = 1; j < 6; j++)
            {
                string name = zoneName[i - 1, j - 1];
                if(currentCollisions[name].Count != 0)
                {
                    Debug.Log(name + " " + i + " " + j + " : " + currentCollisions[name].Count);
                    foreach (GameObject vehicle in currentCollisions[name])
                    {
                        Vector2 otherSpeed = vehicle.GetComponent<SpeedMonitor>().speed;
                        float angle = Vector2.Angle(mainCarSpeed, otherSpeed);
                        Debug.Log("Angle: " + angle);
                    }                  
                }             
            }
        }
    }

    public void PublishZoneContents()
    {
        Vector2 mainCarSpeed = GetComponent<SpeedMonitor>().speed;
        StringBuilder sb = new StringBuilder("[");

        for (int i = 1; i < 6; i++)
        {
            for (int j = 1; j < 6; j++)
            {
                string name = zoneName[i - 1, j - 1];         
              
                if (currentCollisions[name].Count != 0)
                {
                    sb.Append("[" + '"' + name + '"' + " ,");
                    foreach (GameObject vehicle in currentCollisions[name])
                    {                 
                        Vector2 otherSpeed = vehicle.GetComponent<SpeedMonitor>().speed;
                        float angle = Vector2.Angle(mainCarSpeed, otherSpeed);
                        sb.Append("[" + '"' + vehicle.name + '"' + " , " + angle + "],");
                        
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("],");
                } else
                {
                    sb.Append("[" + '"' + name + '"' + "],");
                }
            }
        }
        sb.Remove(sb.Length - 1, 1);
        sb.Append("]");

        string output = sb.ToString();
        this.connection.GetRedisProvider().SendCommand(
            TeamDev.Redis.RedisCommand.SET,
            ZonesKey, output);
        this.connection.GetRedisProvider().SendCommand(
                TeamDev.Redis.RedisCommand.PUBLISH,
                ZonesKey, output);
        
        //this.connection.GetRedisProvider().SendCommand(
        //    TeamDev.Redis.RedisCommand.XADD,
        //    "Unity", "*", 
        //   "zones", sb.ToString());
    }
}
