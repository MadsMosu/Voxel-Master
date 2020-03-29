using UnityEngine;

public class OctreeTestObject : MonoBehaviour {

    private Octree octree;

    void Start () {
        octree = new Octree (16, 10);
    }

    void OnDrawGizmos () {
        if (octree != null)
            octree.DrawLeafNodes ();
    }

}