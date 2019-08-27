using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class List3D<T>
{

    private List<T> list = new List<T>();
    private int minX, maxX, minY, maxY, minZ, maxZ;

    public List3D()
    {



    }


    public void Add(Index3D index, T value)
    {
        minX = Mathf.Min(index.x, minX);
        minY = Mathf.Min(index.y, minY);
        minZ = Mathf.Min(index.z, minZ);
        maxX = Mathf.Max(index.x, maxX);
        maxY = Mathf.Max(index.y, maxY);
        maxZ = Mathf.Max(index.z, maxZ);

    }


    //public T Get(Index3D index)
    //{

    //}



    public int GetIndex(int x, int y, int z)
    {
        return x + ((maxX - minX) * y) + (((maxX - minX) * (maxY - minY)) * z);

    }

}

public struct Index3D
{
    public int x, y, z;

    public Index3D(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

}
