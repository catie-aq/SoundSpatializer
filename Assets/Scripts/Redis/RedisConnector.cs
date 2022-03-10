using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;
using System;

public class RedisConnector : MonoBehaviour, IRedisAutoconnect
{

    // public GameObject RedisConnection;
    public RedisConnection connection;

    // Get loop example
    public float cubeScale;
    public bool IsRunning;

    // Pub/sub example
    public Light MyLight;

    private string newColor = "";
    private bool isNewColor;

    void Awake()
    {
        // Declare itself to the Redis manager
        connection.AddAutoConnect(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Update Every X seconds.
        StartCoroutine(UpdateScale());
    } 

    // Update is called once per frame
    void Update()
    {
        if (isNewColor) {
           ColorSetter();
           isNewColor = false;
        }   
    }

    // Register the SUBSCRIBE connections
    public void RegisterListeners(RedisDataAccessProvider redis)
    {
       redis.MessageReceived += new MessageReceivedHandler(MessageReceived);
        // this.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler(MyMessageHandler);

        string[] channels = new string[3] { "red", "green", "blue" };
        redis.Messaging.Subscribe(channels);
    }

    private void ColorSetter()
    {     
        if (newColor.Equals("red"))
        {
            MyLight.color = Color.red; //  float.Parse(value);
        }
        if (newColor.Equals("green"))
        {
            MyLight.color = Color.green; //  float.Parse(value);
        }
        if (newColor.Equals("blue"))
        {
            MyLight.color = Color.blue; //  float.Parse(value);
        }
    }

    /**
     * Update every X seconds
    */
    IEnumerator UpdateScale()
    {
        while (IsRunning)
        {
            if (connection.IsReady())
            {
                try
                {
                    var redis = connection.GetRedisProvider();
                    string myValueStr = redis.ReadString(redis.SendCommand(RedisCommand.GET, "scale"));
                    cubeScale = float.Parse(myValueStr);
                    this.transform.localScale = new Vector3(cubeScale, cubeScale, cubeScale);
                }catch(ArgumentNullException exception)
                {
                    Debug.Log("No scale value found: " + exception);
                }catch (Exception exception)
                {
                    Debug.Log("Exception in connection / parsing: " + exception);
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Receive a message when there is a publish command. 
    /// Warning: This is in another thread, do not access gameobjects or any object properties directly here.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="value"></param>
    public void MessageReceived(string channel, string value)
    {
        Debug.Log("Message received from " + channel + ", with value : " + value);

        newColor = channel;
        isNewColor = true;
    }

    // public void MyMessageHandler(string channel, byte[] data)
    // {
    //    Debug.Log("Message hander takes from " + channel);
    // }
}
