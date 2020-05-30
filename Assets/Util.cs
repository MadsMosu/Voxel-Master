using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Util {
    public static IEnumerable<Type> GetEnumerableOfType<T> () where T : class {
        List<Type> objects = new List<Type> ();
        foreach (Type type in
            Assembly.GetAssembly (typeof (T)).GetTypes ()
            .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf (typeof (T)))) {
            objects.Add (type);
        }
        return objects;
    }

    public static IEnumerable<object> getEnumerableOfProperties<T> (Type classType, StringBuilder propertyType) {
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return classType.GetType ().GetFields (bindingFlags).
        Where (field => field.FieldType.ToString () == propertyType.ToString ()).ToArray ();
    }

    public static int Map3DTo1D (int x, int y, int z, int size) {
        return x + y * size + z * size * size;
    }

    public static int Map3DTo1D (int x, int y, int z, int sizeX, int sizeY, int sizeZ) {
        return (sizeX * sizeY * z) + (sizeX * y) + x;
    }

    public static Vector3Int Map1DTo3D (int i, int size) {
        var x = i % size;
        var y = (i / size) % size;
        var z = i / (size * size);
        return new Vector3Int (x, y, z);
    }

    public static Vector3Int Map1DTo3D (int i, int sizeX, int sizeY, int sizeZ) {
        var x = i % sizeX;
        var y = (i / sizeX) % sizeY;
        var z = i / (sizeX * sizeY);
        return new Vector3Int (x, y, z);
    }

    public static int Map2DTo1D (int x, int y, int sizeX) {
        return x + sizeX * y;
    }

    public static Vector2Int Map1DTo2D (int i, int sizeX) {
        var x = i % sizeX;
        var y = i / sizeX;
        return new Vector2Int (x, y);
    }

    public static Vector3Int FloorVector3 (Vector3 v) {
        return new Vector3Int ((int) v.x, (int) v.y, (int) v.z);
    }

    public static T CreateInstance<T> (String type) {
        return (T) Activator.CreateInstance (Type.GetType (type));
    }

    public static string FormatClassName (string className) {
        var regex = new Regex (@"(?<!^)[A-Z]+");
        return regex.Replace (className, " $&");
    }

    public static int Int_floor_division (int value, int divider) {
        int q = value / divider;
        if (value % divider < 0) return q - 1;
        else return q;
    }

}