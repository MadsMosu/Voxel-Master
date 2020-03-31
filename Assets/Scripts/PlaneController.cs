using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneController : MonoBehaviour {

    public float forwardSpeed = 3f;
    public float pitchSpeed = 10f;
    public float rollSpeed = 10f;

    public float velocity = 1;

    // Start is called before the first frame update
    void Start () {

    }

    // Update is called once per frame
    void Update () {
        // transform.position += transform.forward * Time.deltaTime * forwardSpeed;
        transform.position += transform.forward * Time.deltaTime * velocity;

        velocity += (Vector3.Dot (transform.forward, -Vector3.up) + .3f) * Time.deltaTime * 5;
        velocity = Mathf.Clamp (velocity, 5, 20);

        transform.rotation *= Quaternion.Euler (Input.GetAxis ("Vertical") * Time.deltaTime * pitchSpeed * (velocity / 20f), 0, -Input.GetAxis ("Horizontal") * Time.deltaTime * rollSpeed * (velocity / 20f));
    }
}