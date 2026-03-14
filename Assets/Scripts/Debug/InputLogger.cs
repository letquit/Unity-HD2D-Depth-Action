using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.InputSystem;

public class InputLogger : MonoBehaviour
{
    private const string FileName = "InputLog.txt";
    private string logFilePath;
    private StringBuilder sb = new StringBuilder();

    void Awake()
    {
        logFilePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ClearLogOnStart()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "InputLog.txt");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            foreach (var key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    LogInput(key.displayName);
                }
            }
        }
        
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) LogInput("Mouse Left");
            if (Mouse.current.rightButton.wasPressedThisFrame) LogInput("Mouse Right");
        }
    }

    void LogInput(string keyName)
    {
        string time = System.DateTime.Now.ToString("HH:mm:ss.fff");
        
        sb.Clear();
        sb.Append("[");
        sb.Append(time);
        sb.Append("] 按下: ");
        sb.Append(keyName);

        string logLine = sb.ToString();

        try
        {
            File.AppendAllText(logFilePath, logLine + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError("无法写入Log文件(请检查管理员权限): " + e.Message);
        }
    }
}