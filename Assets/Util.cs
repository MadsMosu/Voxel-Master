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

    public static int Map3DTo1D (Vector3Int coords, int size) {
        return coords.x + coords.y * size + coords.z * size * size;
    }

    public static Vector3Int Map1DTo3D (int i, int size) {
        var x = i % size;
        var y = (i / size) % size;
        var z = i / (size * size);
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

    public static T CreateInstance<T> (String type) {
        return (T) Activator.CreateInstance (Type.GetType (type));
    }

    public static string FormatClassName (string className) {
        var regex = new Regex (@"(?<!^)[A-Z]+");
        return regex.Replace (className, " $&");
    }

}