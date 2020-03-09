using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneController : MonoBehaviour {

    public float forwardSpeed = 0f;
    public float pitchSpeed = 0f;
    public float rollSpeed = 0f;

    // Start is called before the first frame update
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        // transform.position += transform.forward * Time.deltaTime * forwardSpeed;

        // transform.rotation *= Quaternion.Euler (Input.GetAxis ("Vertical") * Time.deltaTime * pitchSpeed, 0, -Input.GetAxis ("Horizontal") * Time.deltaTime * rollSpeed);
    }
}