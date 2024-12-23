using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(CameraStreamReader))]
public class CameraStreamReaderEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraStreamReader current = (CameraStreamReader)target;
        if (GUILayout.Button("Start Listening")) current.StartListening();
        if (GUILayout.Button("Stop Listening")) current.StopListening();
        if (GUILayout.Button("Restart Listening")) current.RestartListening(null);
    }
}
