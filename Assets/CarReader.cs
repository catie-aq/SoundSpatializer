using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using SimpleJSON;
using UnityEngine;
using System;

public class CarReader : MonoBehaviour, IRedisAutoconnect
{
  
    public RedisConnection connection;

    public string CarKey = "cars";
    private string carData;
    private bool newData;

    
    public GameObject EgoCar;
    public GameObject AllCars;
    public GameObject CarPrefab;
    public GameObject PedestrianPrefab;
    public GameObject BikePrefab;

    void Awake()
    {
        // Declare itself to the Redis manager
        connection.AddAutoConnect(this);
    }

    public void RegisterListeners(RedisDataAccessProvider redis)
    {
        redis.MessageReceived += new MessageReceivedHandler(CarMessageReceived);
        redis.Messaging.Subscribe(new string[1] { CarKey });
    }

    public void CarMessageReceived(string key, string value)
    {
        if (key.Equals(CarKey))
        {
            carData = value;
            newData = true;
      
        }
    }
    protected void UpdateACECars(string newSample){
      try{
        var cars = JSON.Parse(newSample);
        
        //Debug.Log(cars);
        var angle = cars["angle"].AsFloat;
        // Debug.Log(angle);

        EgoCar.transform.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

        System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";
        System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

        // Cleaning old cars.
        foreach(JSONNode deadCar in cars["deadCarNames"]) {
            GameObject vehicle = GameObject.Find(deadCar);
            if(vehicle) 
                Destroy(vehicle);
        }

        foreach(JSONNode car in cars["cars"]) {
            // TODO:  Find child in all cars. 
            // If none create one. 
            //Debug.Log("name: " + car);
            string carName = car["name"];
            // if(carName == "ego_car"){
            //     continue;
            // } 

            Vector3 position = car["position"].ReadVector3();

            GameObject vehicle = GameObject.Find(carName);
            if(!vehicle)
            {
                
                if(carName.StartsWith("bike_")){
                  vehicle = Instantiate(BikePrefab);
                }
                
                if(carName.StartsWith("pedestrian_")){
                  vehicle = Instantiate(PedestrianPrefab);
                }

                if(carName.StartsWith("car_")){
                  vehicle = Instantiate(CarPrefab);
                }        
                if(vehicle == null){
                  vehicle = Instantiate(CarPrefab);
                }
                vehicle.name = carName;
                vehicle.transform.SetParent(AllCars.transform);
            }
            // Debug.Log("position: " + position);

            vehicle.transform.localPosition = position;
        }

        }catch(Exception e){
            Debug.Log(e);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
      if (newData)
        {
            newData = false;
            UpdateACECars(carData);
        }
    }
}
