using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class MarkerInfo
{
    public int id;
    public Vector2[] corners;
    public Vector3 rotation;
    public Vector3 translation;

    public static MarkerInfo[] CreateListJSON(JSONNode markerIDs, JSONNode markerCorners, JSONNode markerRotations, JSONNode markerTranslations)
    {
        //  C# needs to know that DOTS is the universal decimal point. 
        System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";
        System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

        MarkerInfo[] markers = new MarkerInfo[markerIDs.Count];
        for (int i = 0; i < markerIDs.Count; i++)
        {
            MarkerInfo marker = new MarkerInfo();
            marker.id = int.Parse(markerIDs[i]);

            marker.corners = new Vector2[4];
            for (int j = 0; j < 4; j++)
            {
                marker.corners[j] = markerCorners[i][j].ReadVector2();
            }
            marker.rotation = markerRotations[i].ReadVector3();
            marker.translation = markerTranslations[i].ReadVector3();
            markers[i] = marker;
            //Debug.Log(marker.ToString());
        }
        return markers;
    }

    public static MarkerInfo[] CreateList(string ids, string corners, string rotation, string translation)
    {
        var markerIDs = JSON.Parse(ids);
        var markerCorners = JSON.Parse(corners);
        var markerRotations = JSON.Parse(rotation);
        var markerTranslations = JSON.Parse(translation);
        return CreateListJSON(markerIDs, markerCorners, markerRotations, markerTranslations);
    }
    
    public override string ToString()
    {
        return "Marker: " + id + " , screen: " + corners[0].x + " - " + corners[0].y +
            " rotation: " + rotation.x + " " + rotation.y + " " + rotation.z + 
            " translation: " + translation.x + " " + translation.y + " " + translation.z + " . ";
    }
}