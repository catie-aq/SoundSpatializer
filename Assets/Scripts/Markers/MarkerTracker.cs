using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;
using System;
using SimpleJSON;

public class MarkerTracker : MonoBehaviour, IRedisAutoconnect
{
    // public GameObject RedisConnection;
    public RedisConnection connection;
    private RedisDataAccessProvider redis;
    public Camera ARCamera;
    public GameObject MarkerPrefab;
    public float refreshTime = 1f;
    public float focalX = 2000;
    public float focalY = 2000;
    public float cx = 1920 / 2, cy = 1080 / 2;
    public Vector2 resolution = new Vector2(1920, 1080);
    public bool runOnce = true;
    public float markerWidth = 0.0025f;

    private bool newMarkerData = false;
    private string markerData = "[]";

    void Awake()
    {
        connection.AddAutoConnect(this);
       
    }

    private string StripBrackets(string input)
    {
        string withoutFirst = input.Substring(1);
        return withoutFirst.Remove(withoutFirst.Length - 1);
    }

    public void Update()
    {
        if (runOnce)
        {
            StartCoroutine(GetCameraInfos());
            runOnce = false;
        }
        ParseMarkerData();
       // UpdateFOV();
   }

    private void UpdateFOV()
    {
        float vfov = 2.0f * Mathf.Atan(0.5f * resolution.y / focalY) * Mathf.Rad2Deg; // virtual camera (pinhole type) vertical field of view                
        ARCamera.fieldOfView = vfov;
        ARCamera.aspect = resolution.x / resolution.y; // you could set a viewport rect with proper aspect as well... I would prefer the viewport approach
    }

    private void ParseMarkerData()
    {
        if (!newMarkerData)
        {
            return;
        }

        newMarkerData = false;
        try
        {
            var JSONData = JSON.Parse(markerData);
            JSONNode ids = JSONData[0][0];
            JSONNode corners = JSONData[1][0];
            JSONNode rotations = JSONData[2][0];
            JSONNode translations = JSONData[3][0];
            MarkerInfo[] markers = MarkerInfo.CreateListJSON(ids, corners, rotations, translations);
            UpdateMarkerPositions(markers);     
        }
        catch (Exception exception)
        {
            Debug.LogError("Exception in connection / parsing: " + exception);
        }
    }

    private void UpdateMarkerPositions(MarkerInfo[] markers)
    {
        GameObject[] markerObjects = GameObject.FindGameObjectsWithTag("ARucoMarker");
        List<GameObject> markerList = new List<GameObject>(markerObjects);

        /*
        var objects = new Dictionary<int, GameObject>(markerObjects.Length);
        foreach(GameObject gameObject in markerObjects)
        {
            objects[gameObject.GetComponent<PoseUpdater>().id] = gameObject;
        }*/

        foreach (MarkerInfo marker in markers)
        {
            string name = "Marker" + marker.id;
            GameObject markerObject = GameObject.Find(name);
            
            if (markerList.Contains(markerObject))
            {
                markerObject.GetComponent<PoseUpdater>().UpdatePose(marker);
            }else
            {
                markerObject = Instantiate(MarkerPrefab);
                markerObject.name = name;
                markerObject.transform.localScale = new Vector3(markerWidth, markerWidth, markerWidth);
          
                markerObject.GetComponent<PoseUpdater>().UpdatePose(marker);
            }
            markerList.Remove(markerObject);
        }
        foreach(GameObject gameObject in markerList)
        {
            gameObject.GetComponent<PoseUpdater>().CheckPosition();
        }
    }

    IEnumerator GetMarkerList()
    {
        if (connection.IsReady())
        {
            try
            {
                // TODO: Use same API as pub/sub
                redis = connection.GetRedisProvider();
                string ids = StripBrackets(redis.ReadString(redis.SendCommand(RedisCommand.GET, "marker-ids")));
                string corners = StripBrackets(redis.ReadString(redis.SendCommand(RedisCommand.GET, "marker-corners")));
                string rotations = StripBrackets(redis.ReadString(redis.SendCommand(RedisCommand.GET, "marker-rotations")));
                string translations = StripBrackets(redis.ReadString(redis.SendCommand(RedisCommand.GET, "marker-translations")));

                MarkerInfo[] markers = MarkerInfo.CreateList(ids, corners, rotations, translations);
               
                foreach (MarkerInfo marker in markers)
                {
                    string name = "Marker" + marker.id;
                    GameObject markerObject = GameObject.Find(name);
                    if (!markerObject)
                    {
                        markerObject = Instantiate(MarkerPrefab);
                        markerObject.name = name;
                        markerObject.GetComponent<PoseUpdater>().UpdatePose(marker);
                    }
                    markerObject.GetComponent<PoseUpdater>().UpdatePose(marker);
                }
            }
            catch (ArgumentNullException exception)
            {
                Debug.Log("No scale value found: " + exception);
            }
            catch (Exception exception)
            {
                Debug.Log("Exception in connection / parsing: " + exception);
            }
        }
        yield return null;
    }

    IEnumerator GetCameraInfos()
    {
        if (connection.IsReady())
        {
            try
            {
                redis = connection.GetRedisProvider();
                string cameraCalib = redis.ReadString(redis.SendCommand(RedisCommand.GET, "cameraCalibration"));
                var calib = JSON.Parse(cameraCalib);

                System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

                Debug.Log(calib[1]);
                focalX = calib[0].ReadVector3().x;
                focalY = calib[1].ReadVector3().y;
                UpdateFOV();

                string width = redis.ReadString(redis.SendCommand(RedisCommand.GET, "markerWidth"));
                this.markerWidth = float.Parse(width) / 10f; 
            }
            catch (Exception exception)
            {
                Debug.Log("Exception in connection / parsing: " + exception);
            }
        }else
        {
            Debug.Log("Impossible to fetch the camera calibration, default values used");
        }

        yield return null;   
    }


    public void RegisterListeners(RedisDataAccessProvider redis)
    {
        Debug.Log("Register video listener");
        redis.MessageReceived += new MessageReceivedHandler(MessageReceived);
        string[] channels = new string[1] { "markerData" };
        redis.Messaging.Subscribe(channels);
    }

    public void MessageReceived(string channel, string value)
    {
        // Debug.Log("Message received from " + channel + ", with value : " + value);
        // newColor = channel;

        if (channel.Equals("markerData"))
        {
            newMarkerData = true;
            markerData = value;
        }
     
        
    }
}
