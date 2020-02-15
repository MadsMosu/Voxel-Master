using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class Util
{
    public static IEnumerable<T> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class
    {
        List<T> objects = new List<T>();
        foreach (Type type in
            Assembly.GetAssembly(typeof(T)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
        {
            objects.Add((T)Activator.CreateInstance(type, constructorArgs));
        }
        return objects;
    }

    public static IEnumerable<Object> getEnumerableOfProperties<T>(Type classType, StringBuilder propertyType)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return classType.GetType().GetFields(bindingFlags).
            Where(field => field.FieldType.ToString() == propertyType.ToString()).ToArray();
    }
}

