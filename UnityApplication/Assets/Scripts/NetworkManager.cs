using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    public string url;
    private HttpClient client;
    private ImageTest pixelSensor;

    public Transform cameraSetup;
    public Transform headset;

    [Serializable]
    public class Request 
    {
        public Request(string imageData, Vector3 position, Quaternion rotation)
        {
            this.imageData = imageData;
            pos = new List<float> { position.x, position.y, position.z };
            rot = new List<float> { rotation.x, rotation.y, rotation.z, rotation.w };
        }

        public string imageData;
        public List<float> pos;
        public List<float> rot;

        public Vector3 getPos()
        {
            if (pos == null || pos.Count != 3)
                return Vector3.zero;
            return new Vector3(pos[0], pos[1], pos[2]);
        }

        public Quaternion getRot()
        {
            if (rot == null || rot.Count != 4)
                return Quaternion.identity;
            return new Quaternion(rot[0], rot[1], rot[2], rot[3]).normalized;
        }
    }
    [Serializable]
    public class Response
    {
        public List<float> pos;
        public List<float> rot;

        public Vector3 getPos()
        {
            if (pos == null || pos.Count != 3)
                return Vector3.zero;
            return new Vector3(pos[0], pos[1], pos[2]);
        }

        public Quaternion getRot()
        {
            if (rot == null || rot.Count != 4)
                return Quaternion.identity;
            return new Quaternion(rot[0], rot[1], rot[2], rot[3]).normalized;
        }
    }

    [Serializable]
    public class Pose
    {
        public float timeStamp;
        public Vector3 position;
        public Quaternion rotation;

        public Pose (Vector3 position, Quaternion rotation)
        {
            this.timeStamp = Time.realtimeSinceStartup;
            this.position = position;
            this.rotation = rotation;
        }

        public float GetDistance(Pose other)
        {
            return Vector3.Distance(position, other.position);
        }

        public float GetRotationAngle(Pose other)
        {
            return Quaternion.Angle(rotation, other.rotation);
        }
    }
    private List<Pose> lastPoses = new List<Pose>();

    void Start()
    {
        client = new HttpClient();
        pixelSensor = FindAnyObjectByType<ImageTest>();

        StartCoroutine(DoPositionUpdates());
    }

    public IEnumerator DoPositionUpdates()
    {
        while (Application.isPlaying)
        {
            float distPos, distRot;
            do
            {
                yield return null;
                GetDistances(out distPos, out distRot);
                LoggerUtility.instance.Log($"Dist: {distPos}, Rot: {distRot}");
            } while (distPos > 0.1f || distRot > 10.0f);

            if (pixelSensor.TryGetPixelData(out Request rq))
            {
                Task<Response> res = DoAsyncPost(url, rq);
                LoggerUtility.instance.Log("Sending Request");

                if (res == null)
                {
                    LoggerUtility.instance.LogError("The task was null");
                    continue;
                }

                float time = Time.timeSinceLevelLoad;
                while (true)
                {
                    if (Time.timeSinceLevelLoad - time > 20)
                    {
                        LoggerUtility.instance.Log("Timed out");
                        break;
                    }
                    if (res.IsCompleted)
                    {
                        LoggerUtility.instance.Log("Task completed");
                        break;
                    }
                    yield return null;
                }

                if (!res.IsCompleted)
                {
                    //Timed out
                    continue;
                }

                if (res.IsFaulted)
                {
                    LoggerUtility.instance.Log("Task Faulted");
                    yield return new WaitForSeconds(5);
                    continue;
                }
                if (res.IsCanceled)
                {
                    LoggerUtility.instance.Log("Task Canceled");
                    yield return new WaitForSeconds(5);
                    continue;
                }

                if (res.IsCompletedSuccessfully)
                {
                    LoggerUtility.instance.Log("Task Completed Successfully");
                }

                Response r = res.Result;
                LoggerUtility.instance.Log("Got Response");
                if (r == null)
                {
                    LoggerUtility.instance.Log("Response was not parseable");
                    yield return new WaitForSeconds(5);
                    continue;
                }

                MovePlayer(rq.getPos(), rq.getRot(), r.getPos(), r.getRot());
                LoggerUtility.instance.Log("Adjusted the player");
                yield return new WaitForSeconds(5);
                continue;
            }
            else
            {
                LoggerUtility.instance.Log("Couldn't get pixel data");
                yield return new WaitForSeconds(5);
                continue;
            }
        }
    }

    public async Task<Response> DoAsyncPost(string url, Request item)
    {
        try
        {
            Debug.Log(JsonConvert.SerializeObject(item));
            var result = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json"));

            if (!result.IsSuccessStatusCode)
            {
                LoggerUtility.instance.Log(result.StatusCode.ToString());
                return null;
            }

            var stringresult = await result.Content.ReadAsStringAsync();
            Debug.Log(stringresult);

            return JsonConvert.DeserializeObject<Response>(stringresult);
        }
        catch (Exception ex) { 
            LoggerUtility.instance.LogError(ex.Message);
            return null;
        }
    }

    public void MovePlayer(Vector3 currPos, Quaternion currRot, Vector3 targetPos, Quaternion targetRot)
    {
        cameraSetup.SetPositionAndRotation(targetPos, targetRot);
        //App.AddCameraMarker(targetPos, 10.0f);
    }

    public void Update()
    {
        lastPoses.Add(new Pose(headset.localPosition, headset.localRotation));

        while(lastPoses.Count != 0 && lastPoses[0].timeStamp < Time.realtimeSinceStartup - 0.5f)
        {
            lastPoses.RemoveAt(0);
        }
    }

    public void GetDistances(out float distancePosition, out float distanceRotation)
    {
        distancePosition = 0;
        distanceRotation = 0;

        for(int i = 1; i < lastPoses.Count; i++)
        {
            distancePosition += lastPoses[i - 1].GetDistance(lastPoses[i]);
            distanceRotation += lastPoses[i - 1].GetRotationAngle(lastPoses[i]);
        }
    }
}
