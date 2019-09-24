using UnityEngine;

class FirstPersonController : MonoBehaviour
{
    public Camera camera;

    public float amount;

    public VoxelGrid voxelWorld;
    void Start()
    {

    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Debug.DrawLine(ray.origin, hit.point, Color.green);
                voxelWorld.addDensity(hit.point, amount);           
            }
        }
    }
}