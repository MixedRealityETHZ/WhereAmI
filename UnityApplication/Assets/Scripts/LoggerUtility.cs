using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class LoggerUtility : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    public static LoggerUtility instance;

    [SerializeField]
    private int CharacterLimit = 800;

    public void Awake()
    {
        instance = this;
    }

    public void Log(string message)
    {
        text.text = message + "\n" + text.text;
        text.text = text.text.Substring(0, Mathf.Min(text.text.Length, CharacterLimit));
    }

    public void LogError(string message)
    {
        text.text = "<color=red>" + message + "</color>\n" + text.text;
        text.text = text.text.Substring(0, Mathf.Min(text.text.Length, CharacterLimit));
    }
}
