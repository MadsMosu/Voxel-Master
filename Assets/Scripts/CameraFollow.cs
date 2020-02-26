using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public Transform target;
    public Vector3 followOffset = Vector3.zero;

    // Start is called before the first frame update
    void Start () {
        if (target == null) Destroy (this);
    }

    // Update is called once per frame
    void Update () {
        var localOffset = target.TransformVector (followOffset);
        transform.position = Vector3.Lerp (transform.position, target.position + localOffset, Time.deltaTime * 5);
        transform.LookAt (target);
    }
}