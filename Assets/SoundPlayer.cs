using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{

    private AudioSource source;
    private Transform CameraTransform;

    // Start is called before the first frame update
    void Start()
    {
        source = GetComponent<AudioSource>();
        CameraTransform = GameObject.Find("Main Camera").transform;
    }

    // Update is called once per frame
    void Update()
    {
        float dist = Vector3.Distance(this.transform.position, CameraTransform.position);

       // Debug.Log("Distance: " + dist);
      
    }
}
