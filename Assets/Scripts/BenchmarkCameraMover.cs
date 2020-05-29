using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BenchmarkCameraMover : MonoBehaviour {
    public string filename;
    public float startSpeed = 1f;
    public float endSpeed = 1f;

    public float rampTime = 60;
    private float currentSpeed = 0f;
    float timer = 0f;
    int seconds = 0;
    int prevSecond;
    bool minutePassed = false;

    StreamWriter writer;

    // Start is called before the first frame update
    void Start () {
        DebugGUI.AddVariable ("Benchmark Timer", () => timer);
        Debug.Log (Application.persistentDataPath + "constant_optimized.csv");
        writer = new StreamWriter (Application.persistentDataPath + "/" + filename);
        writer.WriteLine ("FPS,Seconds,Speed");
    }

    // Update is called once per frame
    void Update () {
        timer += Time.smoothDeltaTime;
        float percent = timer / rampTime;
        currentSpeed = Mathf.Lerp (startSpeed, endSpeed, percent);
        transform.position += new Vector3 (0, 0, -currentSpeed * Time.deltaTime);

        int fps = (int) (1f / Time.unscaledDeltaTime);
        prevSecond = seconds;
        seconds = (int) (timer % rampTime);
        if (seconds <= 60 && seconds != prevSecond) {
            writer.WriteLine ($"{fps},{seconds},{currentSpeed}");
        }
        Debug.Log (seconds);

        if (timer > 60) {
            writer.Flush ();
            writer.Close ();
            Destroy (this);
        }

    }
}