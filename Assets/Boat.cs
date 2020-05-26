using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Boat : MonoBehaviour {

    public Transform waterTransform;

    private Plane waterPlane;
    private new Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start() {
        waterPlane = new Plane(Vector3.up, waterTransform.position);
        rigidbody = GetComponent<Rigidbody>();
        Debug.Log(rigidbody);
    }



    Vector3[] raycastOffsets = new Vector3[] {
        Vector3.forward + Vector3.left,
        Vector3.forward + Vector3.right,
        Vector3.back + Vector3.left,
        Vector3.back + Vector3.right,
    };
    public float floatDistance = 5;
    // Update is called once per frame
    void FixedUpdate() {
        foreach (var offset in raycastOffsets) {
            var ray = new Ray(transform.TransformPoint(offset / 2), -transform.up);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, floatDistance)) {
                var factor = (floatDistance - hit.distance) / floatDistance;
                rigidbody.AddForceAtPosition(hit.normal * factor * 5, ray.origin + offset, ForceMode.Acceleration);
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
            }
            else {
                Debug.DrawRay(ray.origin, ray.direction * floatDistance, Color.white);
            }

        }


        rigidbody.AddRelativeForce(new Vector3(0, 0, Input.GetAxisRaw("Vertical") * 4), ForceMode.Acceleration);
        rigidbody.AddTorque(new Vector3(0, Input.GetAxisRaw("Horizontal") * 0.2f, 0), ForceMode.Acceleration);

    }
}
