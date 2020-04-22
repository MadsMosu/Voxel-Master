using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneTest : MonoBehaviour {
    private struct Triangle {
        public Vector3 a, b, c;
    }
    Triangle triangle;
    Plane a, b, c, d, e, f, g;
    // Start is called before the first frame update
    void Start () {
        triangle = new Triangle {
            a = new Vector3 (0, 2, 0),
            b = new Vector3 (4, 8, 0),
            c = new Vector3 (8, 2, 0)
        };
        a = new Plane (triangle.a, triangle.b, triangle.c);
        b = new Plane ((triangle.c - triangle.a) * (1.0f / Vector3.Distance (triangle.a, triangle.c)), triangle.a);
        c = new Plane ((triangle.b - triangle.c) * (1.0f / Vector3.Distance (triangle.c, triangle.b)), triangle.c);
        d = new Plane ((triangle.a - triangle.b) * (1.0f / Vector3.Distance (triangle.b, triangle.a)), triangle.b);
        e = new Plane (Vector3.Cross (a.normal, triangle.c - triangle.a), (triangle.a + triangle.b) / 2);
        f = new Plane (Vector3.Cross (a.normal, triangle.b - triangle.c), (triangle.b + triangle.c) / 2);
        g = new Plane (Vector3.Cross (a.normal, triangle.a - triangle.b), (triangle.c + triangle.a) / 2);
    }

    // Update is called once per frame
    void Update () {

    }

    void OnDrawGizmos () {

        Color planeColor;
        // TRIANGLE

        planeColor = Color.green;
        planeColor.a = 0.1f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine (triangle.a, triangle.b);
        Gizmos.DrawLine (triangle.b, triangle.c);
        Gizmos.DrawLine (triangle.c, triangle.a);

        //PLANE A
        planeColor = Color.red;
        planeColor.a = 0.25f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine ((triangle.a + triangle.b + triangle.c) / 3, ((triangle.a + triangle.b + triangle.c) / 3) + a.normal);

        //PLANE B
        planeColor = Color.cyan;
        planeColor.a = 0.40f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine (triangle.a, triangle.a + b.normal);

        //PLANE C
        planeColor = Color.magenta;
        planeColor.a = 0.40f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine (triangle.c, triangle.c + c.normal);

        //PLANE d
        planeColor = Color.yellow;
        planeColor.a = 0.40f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine (triangle.b, triangle.b + d.normal);

        // PLANE E
        planeColor = Color.gray + Color.green;
        planeColor.a = 0.40f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine ((triangle.a + triangle.c) / 2, ((triangle.a + triangle.c) / 2) + -e.normal);

        // PLANE F
        planeColor = Color.grey + Color.blue;
        planeColor.a = 0.40f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine ((triangle.c + triangle.b) / 2, ((triangle.c + triangle.b) / 2) + -f.normal);

        // PLANE G
        planeColor = Color.gray + Color.red;
        planeColor.a = 0.40f;
        Gizmos.color = planeColor;
        Gizmos.DrawLine ((triangle.b + triangle.a) / 2, ((triangle.b + triangle.a) / 2) + -g.normal);

    }
}