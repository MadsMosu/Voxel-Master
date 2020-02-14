using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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


    public static int Map3DTo1D(UnityEngine.Vector3Int coords, UnityEngine.Vector3Int size)
    {
        return coords.x + size.y * (coords.y + size.z * coords.z);
    }

    public static int Map2DTo1D(UnityEngine.Vector2Int coords, UnityEngine.Vector2Int size)
    {
        return size.x * coords.x + coords.y;
    }
}

