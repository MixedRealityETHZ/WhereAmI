using MagicLeap.Android;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;
using MagicLeap.OpenXR.Features.PixelSensors;
using UnityEngine.XR.OpenXR;
using Unity.VisualScripting;
using Unity.Collections;
using TMPro;
using System;

public class ImageTest : MonoBehaviour
{
    public enum SensorName
    {
        World_Center,
        World_Left,
        World_Right,
        Picture_Center,
        Eye_Temple_Left,
        Eye_Temple_Right,
        Eye_Nasal_Left,
        Eye_Nasel_Right,
        Depth_Center
    }

    [Header("Stream Configuration")]
    public bool useStream0 = true;
    public bool useStream1 = true;

    [Header("Render Settings")]
    [SerializeField] private Renderer[] streamRenderers = new Renderer[2];
    //[SerializeField]
    //[Tooltip("Set to either: World Center, World Left, World Right")]
    //private string pixelSensorName = "World Center";
    private string pixelSensorName
    {
        get { return pixelSensor.ToString().Replace('_', ' '); } 
    }
    [SerializeField] private SensorName pixelSensor;

    private string requiredPermission = UnityEngine.Android.Permission.Camera;
    // Array to hold textures for each stream
    private Texture2D[] streamTextures = new Texture2D[2];
    // Optional sensor ID, used to interact with the specific sensor
    private PixelSensorId? sensorId;
    // List to keep track of which streams have been configured
    private readonly List<uint> configuredStreams = new List<uint>();
    // Reference to the Magic Leap Pixel Sensor Feature
    private MagicLeapPixelSensorFeature pixelSensorFeature;

    void OnEnable()
    {
        InitializePixelSensorFeature();
    }

    private void InitializePixelSensorFeature()
    {
        // Get the Magic Leap Pixel Sensor Feature from the OpenXR settings
        pixelSensorFeature = OpenXRSettings.Instance.GetFeature<MagicLeapPixelSensorFeature>();
        if (pixelSensorFeature == null || !pixelSensorFeature.enabled)
        {
            LoggerUtility.instance.LogError("Pixel Sensor Feature Not Found or Not Enabled!");
            enabled = false;
            return;
        }
        RequestPermission(requiredPermission);
    }

    private void RequestPermission(string permission)
    {
        Permissions.RequestPermission(permission, OnPermissionGranted, OnPermissionDenied);
    }

    private void OnPermissionGranted(string permission)
    {
        if (Permissions.CheckPermission(requiredPermission))
        {
            FindAndInitializeSensor();
        }
    }
    private void OnPermissionDenied(string permission)
    {
        LoggerUtility.instance.LogError($"Permission Denied: {permission}");
        enabled = false;
    }

    private void FindAndInitializeSensor()
    {
        List<PixelSensorId> sensors = pixelSensorFeature.GetSupportedSensors();

        int index = sensors.FindIndex(x => x.SensorName.Contains(pixelSensorName));

        if (index <= 0)
        {
            LoggerUtility.instance.LogError($"{pixelSensorName} sensor not found.");
            return;
        }

        sensorId = sensors[index];

        // Subscribe to sensor availability changes
        pixelSensorFeature.OnSensorAvailabilityChanged += OnSensorAvailabilityChanged;
        TryInitializeSensor();
    }

    private void OnSensorAvailabilityChanged(PixelSensorId id, bool available)
    {
        if (id == sensorId && available)
        {
            LoggerUtility.instance.Log($"Sensor became available: {id.SensorName}");
            TryInitializeSensor();
        }
    }

    private void TryInitializeSensor()
    {
        if (sensorId.HasValue && pixelSensorFeature.CreatePixelSensor(sensorId.Value))
        {
            LoggerUtility.instance.Log("Sensor created successfully.");
            ConfigureSensorStreams();
        }
        else
        {
            LoggerUtility.instance.LogError("Failed to create sensor. Will retry when available.");
        }
    }

    private void ConfigureSensorStreams()
    {
        if (!sensorId.HasValue)
        {
            LoggerUtility.instance.LogError("Sensor Id was not set.");
            return;
        }

        uint streamCount = pixelSensorFeature.GetStreamCount(sensorId.Value);
        if (useStream1 && streamCount < 2 || useStream0 && streamCount < 1)
        {
            LoggerUtility.instance.LogError("target Streams are not available from the sensor.");
            return;
        }

        for (uint i = 0; i < streamCount; i++)
        {
            if ((useStream0 && i == 0) || (useStream1 && i == 1))
            {
                configuredStreams.Add(i);
            }
        }

        StartCoroutine(StartSensorStream());
    }

    // Coroutine to configure stream and start sensor streams
    private IEnumerator StartSensorStream()
    {
        // Configure the sensor with default configuration
        PixelSensorAsyncOperationResult configureOperation =
            pixelSensorFeature.ConfigureSensorWithDefaultCapabilities(sensorId.Value, configuredStreams.ToArray());

        yield return configureOperation;

        if (!configureOperation.DidOperationSucceed)
        {
            LoggerUtility.instance.LogError("Failed to configure sensor.");
            yield break;
        }

        LoggerUtility.instance.Log("Sensor configured with defaults successfully.");

        // Start the sensor with the default configuration and specify that all of the meta data should be requested.
        var sensorStartAsyncResult =
            pixelSensorFeature.StartSensor(sensorId.Value, configuredStreams);

        yield return sensorStartAsyncResult;

        if (!sensorStartAsyncResult.DidOperationSucceed)
        {
            LoggerUtility.instance.LogError("Stream could not be started.");
            yield break;
        }

        LoggerUtility.instance.Log("Stream started successfully.");
        //yield return ProcessSensorData();
    }

    //private IEnumerator ProcessSensorData()
    //{
    //    while (sensorId.HasValue && pixelSensorFeature.GetSensorStatus(sensorId.Value) == PixelSensorStatus.Started)
    //    {
    //        foreach (var stream in configuredStreams)
    //        {
    //            // In this example, the meta data is not used.
    //            if (pixelSensorFeature.GetSensorData(
    //                    sensorId.Value, stream,
    //                    out var frame,
    //                    out PixelSensorMetaData[] currentFrameMetaData,
    //                    Allocator.Temp,
    //                    shouldFlipTexture: true))
    //            {
    //                Pose sensorPose = pixelSensorFeature.GetSensorPose(sensorId.Value);
    //                Debug.Log($"Pixel Sensor Pose: Position {sensorPose.position} Rotation: {sensorPose.rotation}");
    //                ProcessFrame(frame, streamRenderers[stream], streamTextures[stream]);
    //            }
    //        }
    //        yield return new WaitForSeconds(1.0f / 30.0f);
    //    }
    //}
    //
    //public void ProcessFrame(in PixelSensorFrame frame, Renderer targetRenderer, Texture2D targetTexture)
    //{
    //    if (!frame.IsValid || targetRenderer == null || frame.Planes.Length == 0)
    //    {
    //        return;
    //    }
    //
    //    if (targetTexture == null)
    //    {
    //
    //        var plane = frame.Planes[0];
    //        switch (frame.FrameType){
    //            case PixelSensorFrameType.Grayscale:
    //                targetTexture = new Texture2D((int)plane.Width, (int)plane.Height, TextureFormat.R8, false);
    //                break;
    //            case PixelSensorFrameType.Rgba8888:
    //                targetTexture = new Texture2D((int)plane.Width, (int)plane.Height, TextureFormat.RGBA32, false);
    //                break;
    //            case PixelSensorFrameType.Jpeg:
    //                targetTexture = new Texture2D((int)plane.Width, (int)plane.Height, TextureFormat.RGBA32, false);
    //                break;
    //            default:
    //                targetTexture = new Texture2D((int)plane.Width, (int)plane.Height, TextureFormat.R8, false);
    //                break;
    //        }
    //        targetRenderer.material.mainTexture = targetTexture;
    //    }
    //
    //    if(frame.FrameType == PixelSensorFrameType.Jpeg)
    //    {
    //        targetTexture.LoadImage(frame.Planes[0].ByteData.ToArray(), false);
    //    }
    //    else
    //    {
    //        targetTexture.LoadRawTextureData(frame.Planes[0].ByteData);
    //    }
    //
    //    targetTexture.Apply();
    //}

    private void OnDisable()
    {
        var camMono = Camera.main.GetComponent<MonoBehaviour>();
        camMono.StartCoroutine(StopSensor());
    }

    private IEnumerator StopSensor()
    {
        if (sensorId.HasValue)
        {
            var stopSensorAsyncResult = pixelSensorFeature.StopSensor(sensorId.Value, configuredStreams);
            yield return stopSensorAsyncResult;
            if (stopSensorAsyncResult.DidOperationSucceed)
            {
                pixelSensorFeature.DestroyPixelSensor(sensorId.Value);
                LoggerUtility.instance.Log("Sensor stopped and destroyed successfully.");
            }
            else
            {
                LoggerUtility.instance.LogError("Failed to stop the sensor.");
            }
        }
    }

    public bool TryGetPixelData(out NetworkManager.Request request)
    {
#if UNITY_EDITOR
        request = new NetworkManager.Request("", Camera.main.transform.localPosition, Camera.main.transform.localRotation);
        return true;
#endif

        if (sensorId != null && sensorId.HasValue && pixelSensorFeature.GetSensorStatus(sensorId.Value) == PixelSensorStatus.Started)
        {
            if (pixelSensorFeature.GetSensorData(
                        sensorId.Value, 0,
                        out var frame,
                        out PixelSensorMetaData[] currentFrameMetaData,
                        Allocator.Temp,
                        shouldFlipTexture: true))
            {
                Pose pose = pixelSensorFeature.GetSensorPose(sensorId.Value);
                string imageData = Convert.ToBase64String(frame.Planes[0].ByteData);
                request = new NetworkManager.Request(imageData, pose.position, pose.rotation);
                return true;
            }
        }
   
        request = null;
        return false;
    }
}
