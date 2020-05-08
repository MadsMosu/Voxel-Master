using UnityEngine;
using VoxelMaster;

public class CameraRaycast : MonoBehaviour {
    public Camera camera;
    public float amount;

    void Start () {

    }

    void Update () {
        if (Input.GetMouseButtonUp (0)) {
            RaycastHit hit;
            var ray = camera.ScreenPointToRay (Input.mousePosition);
            if (Physics.Raycast (ray, out hit, Mathf.Infinity)) {
                // Debug.DrawLine (ray.origin, hit.point, Color.green);
                if (hit.transform.GetComponent<VoxelObject> () != null) {
                    VoxelObject voxelObject = hit.transform.GetComponent<VoxelObject> ();
                    VoxelSplitter.Split (voxelObject, hit.point);
                }
            }
        }
    }
}