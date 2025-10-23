using UnityEngine;

public class Screenshot : MonoBehaviour
{
    public int superSize = 2; // 1 = resolución actual, 2 = doble, etc.
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            string filename = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            ScreenCapture.CaptureScreenshot(filename, superSize);
            Debug.Log("Captura guardada: " + filename);
        }
    }
}
