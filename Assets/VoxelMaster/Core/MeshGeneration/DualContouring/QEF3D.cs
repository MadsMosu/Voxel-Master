using System.Collections.Generic;
using UnityEngine;

public class QEF3D
{
    public List<Vector3> Intersections { get; set; }
    public List<Vector3> Normals { get; set; }
    public float Error { get; set; }
    Vector3 mass_point;
    private static Vector3[] deltas;

    static QEF3D()
    {
        int size = 8;
        deltas = new Vector3[size * size * size];
        for (int x = 0; x < size; x++)
        {
            float dx = (float)x / (float)(size / 2) - 1.0f;
            for (int y = 0; y < size; y++)
            {
                float dy = (float)y / (float)(size / 2) - 1.0f;
                for (int z = 0; z < size; z++)
                {
                    float dz = (float)z / (float)(size / 2) - 1.0f;
                    deltas[x * size * size + y * size + z] = new Vector3(dx * 0.01f, dy * 0.01f, dz * 0.01f);
                }
            }
        }
    }

    public QEF3D()
    {
        Intersections = new List<Vector3>();
        Normals = new List<Vector3>();
    }

    public void Add(Vector3 p, Vector3 n)
    {
        Intersections.Add(p);
        Normals.Add(n);
        mass_point += p;
    }

    private float GetDistanceSquared(Vector3 x)
    {
        float total = 0;
        for (int i = 0; i < Intersections.Count; i++)
        {
            Vector3 d = x - Intersections[i];
            float dot = Normals[i].x * d.x + Normals[i].y * d.y + Normals[i].z * d.z;
            total += dot * dot;
        }
        return total;
    }

    private Vector3 Clamp(ref Vector3 value, Vector3 min, Vector3 max)
    {
        value.x = Mathf.Clamp(value.x, min.x, max.x);
        value.y = Mathf.Clamp(value.y, min.y, max.y);
        value.z = Mathf.Clamp(value.z, min.z, max.z);
        return value;
    }

    /* Currently disabled; it just returns the mass point, which means sharp features are lost */
    public Vector3 Solve()
    {
        if (Intersections.Count == 0)
        {
            this.Error = 100000;
            return Vector3.zero;
        }
        Vector3 x = mass_point / (float)Intersections.Count;
        float error = GetDistanceSquared(x);
        this.Error = error;
        //return x;

        if (Mathf.Abs(error) >= 0.0001f)
        {
            for (int i = 0; i < deltas.Length; i++)
            {
                Vector3 new_point = new Vector3(x.x + deltas[i].x, x.y + deltas[i].y, x.z + deltas[i].z);
                new_point = Clamp(ref new_point, Vector3.zero, Vector3.one);
                float e = GetDistanceSquared(new_point);
                if (e <= error)
                {
                    x = new_point;
                    if (Mathf.Abs(e) < 0.0001f)
                        break;
                    error = e;
                }
            }
        }

        if (x.x > 1 || x.y > 1 || x.z > 1 || x.x < 0 || x.y < 0 || x.z < 0)
            return mass_point / (float)Intersections.Count;
        return Clamp(ref x, Vector3.zero, Vector3.one);
    }
}