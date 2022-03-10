using Dweiss;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using TeamDev.Redis;
using UnityEngine;

public class RedisConnection : MonoBehaviour {

    public string ClientName = "CATIE";
    public int UpdateCheck = 5; // seconds

	private RedisDataAccessProvider redis;
    private bool ready = false;
    private bool IsRunning = true;

    private List<IRedisAutoconnect> connectedObjects = new List<IRedisAutoconnect>();
    private bool isTryingConnection = false;


    // Connect in start, in Awake there were some issues.
    void Start()
    {
        TryConnection();
        IsRunning = true;
        new Thread(() => { PeriodicCheck(); }).Start();
    }

    public bool IsReady()
    {
        return ready;
    }

    public void AddAutoConnect(IRedisAutoconnect myObject)
    {
        connectedObjects.Add(myObject);
    }

    public RedisDataAccessProvider GetRedisProvider()
    {
        return this.redis;
    }

    public bool TestConnection()
    {
        try {
            string result = redis.ReadString(redis.SendCommand(RedisCommand.PING));
            return result.Equals("PONG");
        }
        catch(Exception e)
        {
            Debug.Log(e);
        }
        return false;
    }

    public bool UniqueTestConnection()
    {
        TryConnection();
        string result = redis.ReadString(redis.SendCommand(RedisCommand.PING));
        return result.Equals("PONG");
    }

    public void TryConnection() {
        if (isTryingConnection)
        {
            // Debug.Log("SKIP - Redis connection in progress...");
            return;
        }
        redis = new RedisDataAccessProvider();
	    redis.Configuration.Host = Settings.Instance.Host;
	    redis.Configuration.Port = Settings.Instance.Port;
        if(Settings.Instance.Password != "")
        {
            redis.Configuration.Password = Settings.Instance.Password;
        }
      
        isTryingConnection = true;
        ready = false;

        try
        {
            // Debug.Log("Creating a new Redis connection... testing: " + connectionTesting.ToString());
            redis.Connect();
            redis.SendCommand(RedisCommand.CLIENT, "SETNAME", "Unity-Client-" + ClientName);
            redis.WaitComplete();

            string myValueStr = redis.ReadString(redis.SendCommand(RedisCommand.CLIENT, "GETNAME"));
            // Debug.Log("Connection OK to server (" + IpAdress + ":" + Port + ").");

            this.redis.ChannelSubscribed += new ChannelSubscribedHandler(OnChannelSubscribed);
            this.redis.ChannelUnsubscribed += new ChannelUnsubscribedHandler(OnChannelUnsubscribed);

            // (Re)connect all the listeners
            foreach (IRedisAutoconnect autoConnectObject in connectedObjects)
            {
                autoConnectObject.RegisterListeners(redis);
            }
            ready = true;
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message + ": Could not connect to redis server (" + Settings.Instance.Host+ ":" + Settings.Instance.Port + ").");
            ready = false;
        }
        finally
        {
            isTryingConnection = false;
        }
	}

    /**
     * Check connection every 5 seconds
    */
    void PeriodicCheck()
    {
        while (IsRunning)
        {
            Debug.Log("Periodic connection test...");
            if (!ready)
            {
                Debug.Log("Redis connection unavailable, trying to create a new one.");
                TryConnection();             
            }

            if (ready && !isTryingConnection)
            {
                bool testResultPositive = TestConnection();
                if (!testResultPositive)
                {
                    Debug.Log("Redis connection died, trying to create a new one.");
                    TryConnection();
                }
            }
 
            Thread.Sleep(UpdateCheck * 1000);
        }
    }

    void OnChannelSubscribed(string channelName)
    {
        Debug.Log("[RedisConnection] SUB to " + channelName);
    }

    void OnChannelUnsubscribed(string channelName)
    {
        Debug.Log("[RedisConnection] UNSUB to " + channelName);
    }
}