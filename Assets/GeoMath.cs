using UnityEngine;

public static class GeoMath {

    public struct Triangle {
        public Vector3 a, b, c;
        public Line3D AB, BA, BC, CB, AC, CA;
        public Triangle (Vector3 a, Vector3 b, Vector3 c) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.AB = new Line3D (a, b);
            this.BA = new Line3D (b, a);
            this.BC = new Line3D (b, c);
            this.CB = new Line3D (c, b);
            this.AC = new Line3D (a, c);
            this.CA = new Line3D (c, a);
        }
        public Vector3 Center () {
            return (a + b + c) / 3;
        }

        public Vector3 SurfaceNormal () {
            Vector3 v = Vector3.Cross (b - a, c - a);
            return v / v.magnitude;
        }

    }

    public struct Line3D {
        public Vector3 p1, p2;
        public Vector3 direction;
        public float length;

        public Line3D (Vector3 p1, Vector3 p2) {
            this.p1 = p1;
            this.p2 = p2;
            this.direction = p2 - p1;
            this.length = Mathf.Sqrt (Mathf.Pow (p2.x - p1.x, 2) + Mathf.Pow (p2.y - p1.y, 2) + Mathf.Pow (p2.z - p1.z, 2));
        }

        public Vector3 Center () {
            return (p1 + p2) / 2;
        }

    }

    public class Plane3D {
        public Vector3 point;
        public Vector3 normal;

        public Plane3D (Vector3 p1, Vector3 p2, Vector3 p3) {
            this.point = p1;
            Triangle t = new Triangle (p1, p2, p3);
            this.normal = t.SurfaceNormal ();
        }

        public Plane3D (Vector3 inNormal, Vector3 inPoint) {
            this.normal = inNormal;
            this.point = inPoint;
        }

        public Plane3D (Line3D line, Vector3 inPoint) {
            this.point = line.p1;
            this.normal = Vector3.Cross (line.direction, inPoint - line.p1);
        }

        public float DistanceToPoint (Vector3 point) {
            //assumes that normal is normalized
            return Vector3.Dot (normal, point - this.point);
        }

        public float DistanceFromOrigin () {
            return DistanceToPoint (new Vector3 (0, 0, 0));
        }

        public void Normalize (float magnitude) {
            if (normal.magnitude > 0) {
                normal = normal / magnitude;
            }
        }

        public void FlipNormal () {
            normal = -normal;
        }
    }

}