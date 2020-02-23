using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


public static class Util
{
    public static IEnumerable<Type> GetEnumerableOfType<T>() where T : class
    {
        List<Type> objects = new List<Type>();
        foreach (Type type in
            Assembly.GetAssembly(typeof(T)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
        {
            objects.Add(type);
        }
        return objects;
    }

    public static IEnumerable<object> getEnumerableOfProperties<T>(Type classType, StringBuilder propertyType)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return classType.GetType().GetFields(bindingFlags).
            Where(field => field.FieldType.ToString() == propertyType.ToString()).ToArray();
    }


    public static int Map3DTo1D(UnityEngine.Vector3Int coords, UnityEngine.Vector3Int size)
    {
        return coords.x + size.y * (coords.y + size.z * coords.z);
    }

    public static UnityEngine.Vector3Int Map1DTo3D(int i, UnityEngine.Vector3Int size)
    {
        var x = i % size.x;
        var y = (i / size.x) % size.y;
        var z = i / (size.x * size.y);
        return new UnityEngine.Vector3Int(x, y, z);
    }

    public static int Map2DTo1D(int x, int y, int sizeX)
    {
        return x + sizeX * y;
    }


    public static Vector2Int Map1DTo2D(int i, int sizeX)
    {
        var x = i % sizeX;
        var y = i / sizeX;
        return new Vector2Int(x, y);
    }


    public static Mesh GeneratePreviewPlane(float[] heights, WorldGeneratorSettings settings)
    {
        var sizeX = settings.worldSize;
        var sizeZ = settings.worldSize;

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[sizeX * sizeZ];
        for (int i = 0, z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++, i++)
            {
                float h;
                try
                {
                    h = heights[Util.Map2DTo1D(x, z, sizeX)];
                }
                catch (System.Exception)
                {
                    h = 0;
                }
                vertices[i] = new Vector3(x, h * 5, z);
            }
        }
        mesh.vertices = vertices;

        sizeX -= 1;
        sizeZ -= 1;

        int[] triangles = new int[sizeX * sizeZ * 6];
        for (int triangleIndex = 0, vertexIndex = 0, y = 0; y < sizeZ; y++, vertexIndex++)
        {
            for (int x = 0; x < sizeX; x++, triangleIndex += 6, vertexIndex++)
            {
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 3] = triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 4] = triangles[triangleIndex + 1] = vertexIndex + sizeX + 1;
                triangles[triangleIndex + 5] = vertexIndex + sizeX + 2;
            }
        }
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.Optimize();

        return mesh;
    }

    public static T CreateInstance<T>(String type)
    {
        return (T)Activator.CreateInstance(Type.GetType(type));
    }

    public static string FormatClassName(string className)
    {
        var regex = new Regex(@"(?<!^)[A-Z]+");
        return regex.Replace(className, " $&");
    }

}