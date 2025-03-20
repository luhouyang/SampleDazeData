using UnityEngine;
using System.Collections;
using Microsoft.MixedReality.Toolkit.SampleGazeData;

public class DataPlayer : MonoBehaviour
{
    public SessionData sessionData;
    public GameObject targetObject;
    public DrawOn3DTexture heatmapVisualizer;

    public void LoadSession(string jsonData)
    {
        sessionData = JsonUtility.FromJson<SessionData>(jsonData);
    }

    public void Playback()
    {
        StartCoroutine(PlaybackRoutine());
    }

    private IEnumerator PlaybackRoutine()
    {
        int gazeIndex = 0;
        int transIndex = 0;

        while (gazeIndex < sessionData.gazeData.Count || transIndex < sessionData.transformData.Count)
        {
            // Playback transforms
            if (transIndex < sessionData.transformData.Count)
            {
                var trans = sessionData.transformData[transIndex];
                targetObject.transform.position = trans.position;
                targetObject.transform.rotation = trans.rotation;
                targetObject.transform.localScale = trans.scale;
                transIndex++;
            }

            // Playback gaze data
            if (gazeIndex < sessionData.gazeData.Count)
            {
                var gaze = sessionData.gazeData[gazeIndex];
                heatmapVisualizer.DrawAtThisHitPos(gaze.hitPosition);
                gazeIndex++;
            }

            yield return new WaitForSeconds(0.01f); // Adjust playback speed
        }
    }
}