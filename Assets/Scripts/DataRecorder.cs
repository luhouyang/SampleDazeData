using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;

[System.Serializable]
public class GazeData
{
    public DateTime timestamp;
    public Vector3 gazeOrigin;
    public Vector3 gazeDirection;
    public Vector3 hitPosition;
}

[System.Serializable]
public class ObjectTransformData
{
    public DateTime timestamp;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

[System.Serializable]
public class SessionData
{
    public List<GazeData> gazeData = new List<GazeData>();
    public List<ObjectTransformData> transformData = new List<ObjectTransformData>();
}

public class DataRecorder : MonoBehaviour
{
    public bool isRecording;
    private SessionData currentSession = new SessionData();
    public KeyboardMouseObjectController selectedObject;

    void Update()
    {
        if (isRecording && EyeTrackingTarget.LookedAtEyeTarget != null)
        {
            RecordGazeData();
            RecordTransformData();
        }
    }

    private void RecordGazeData()
    {
        var gaze = new GazeData
        {
            timestamp = DateTime.UtcNow,
            gazeOrigin = CoreServices.InputSystem.EyeGazeProvider.GazeOrigin,
            gazeDirection = CoreServices.InputSystem.EyeGazeProvider.GazeDirection,
            hitPosition = CoreServices.InputSystem.EyeGazeProvider.HitPosition
        };
        currentSession.gazeData.Add(gaze);
    }

    private void RecordTransformData()
    {
        if (KeyboardMouseObjectController.currentlySelected != null)
        {
            var trans = new ObjectTransformData
            {
                timestamp = DateTime.UtcNow,
                position = KeyboardMouseObjectController.currentlySelected.transform.position,
                rotation = KeyboardMouseObjectController.currentlySelected.transform.rotation,
                scale = KeyboardMouseObjectController.currentlySelected.transform.localScale
            };
            currentSession.transformData.Add(trans);
        }
    }

    public void SaveSession(string fileName)
    {
        string json = JsonUtility.ToJson(currentSession);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/" + fileName, json);
    }
}