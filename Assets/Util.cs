using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

    public static IEnumerable<Object> getEnumerableOfProperties<T>(Type classType, StringBuilder propertyType)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return classType.GetType().GetFields(bindingFlags).
            Where(field => field.FieldType.ToString() == propertyType.ToString()).ToArray();
    }

    public static int Map3DTo1D(UnityEngine.Vector3Int coords, UnityEngine.Vector3Int size)
    {
        return coords.x + size.x * (coords.y + size.y * coords.z);
    }

    public static int Map2DTo1D(UnityEngine.Vector2Int coords, UnityEngine.Vector2Int size)
    {
        return size.x * coords.x + coords.y;
    }

    public static UnityEngine.Vector3Int Map1DTo3D(int index, UnityEngine.Vector3Int size)
    {
        return new UnityEngine.Vector3Int(
            index % size.x,
            (index / size.x) % size.y,
            index / (size.x * size.y)
        );
    }

    public static UnityEngine.Vector2Int Map1DTo2D(int index, UnityEngine.Vector2Int size)
    {
        return new UnityEngine.Vector2Int(
            index % size.y,
            index / size.y
        );
    }
}

