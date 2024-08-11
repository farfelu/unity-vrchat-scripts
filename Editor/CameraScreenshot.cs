using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

public class CameraScreenshot : MonoBehaviour
{
    [MenuItem("Tools/Camera Screenshot")]
    static void CreateCameraScreenshot()
    {
        ScreenCapture.CaptureScreenshot("screenshot.png", 4);
    }
}
