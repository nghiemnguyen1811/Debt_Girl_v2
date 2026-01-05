using UnityEngine;
using System.Collections.Generic;

public class MobileDebug : MonoBehaviour
{
    string myLog = "";
    Queue<string> myLogQueue = new Queue<string>();

    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string newString = "\n [" + type + "] : " + logString;
        myLogQueue.Enqueue(newString);

        // Chỉ giữ lại 15 dòng log gần nhất để không tràn màn hình
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            myLogQueue.Enqueue(newString);
        }

        while (myLogQueue.Count > 15) myLogQueue.Dequeue();

        myLog = string.Empty;
        foreach (string s in myLogQueue) myLog += s;
    }

    void OnGUI()
    {
        // Phóng to chữ để dễ đọc trên điện thoại
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(3, 3, 1));
        GUI.TextArea(new Rect(10, 10, 300, 300), myLog);
    }
}