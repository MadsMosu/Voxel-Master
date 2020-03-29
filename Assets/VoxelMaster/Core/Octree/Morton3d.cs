/***************************************************************************************
 *    Source: https://github.com/Forceflow/libmorton
 ***************************************************************************************/
public class Morton {
    //************************
    //ENCODING
    //************************
    //encodes a 32-bit uint
    static ulong[] magicbit3D_masks64_encode = { 0x1fffff, 0x1f00000000ffff, 0x1f0000ff0000ff, 0x100f00f00f00f00f, 0x10c30c30c30c30c3, 0x1249249249249249 };
    private static ulong morton3D_SplitBy3bits (uint a) {
        ulong x = a & magicbit3D_masks64_encode[0];
        x = (x | x << 32) & magicbit3D_masks64_encode[1]; // for 64-bit case
        x = (x | x << 16) & magicbit3D_masks64_encode[2];
        x = (x | x << 8) & magicbit3D_masks64_encode[3];
        x = (x | x << 4) & magicbit3D_masks64_encode[4];
        x = (x | x << 2) & magicbit3D_masks64_encode[5];

        return x;
    }

    //************************
    //DECODING
    //************************
    //decodes a 32-bit uint
    static ulong[] magicbit3D_masks64_decode = { 0x1fffff, 0x1f00000000ffff, 0x1f0000ff0000ff, 0x100f00f00f00f00f, 0x10c30c30c30c30c3, 0x1249249249249249 };
    private static uint morton3D_GetThirdBits (ulong m) {
        ulong x = m & magicbit3D_masks64_decode[5];
        x = (x ^ (x >> 2)) & magicbit3D_masks64_decode[4];
        x = (x ^ (x >> 4)) & magicbit3D_masks64_decode[3];
        x = (x ^ (x >> 8)) & magicbit3D_masks64_decode[2];
        x = (x ^ (x >> 16)) & magicbit3D_masks64_decode[1];
        x = (x ^ ((ulong) x >> 32)) & magicbit3D_masks64_decode[0];
        return System.Convert.ToUInt32 (x);
    }

    /***************************************************************************************
     *    Source: https://devblogs.nvidia.com/thinking-parallel-part-iii-tree-construction-gpu/
     ***************************************************************************************/

    // Calculates a 64-bit Morton code
    //ENCODE
    public static ulong morton3DEncode (uint x, uint y, uint z) {
        ulong xx = morton3D_SplitBy3bits (x);
        ulong yy = morton3D_SplitBy3bits (y);
        ulong zz = morton3D_SplitBy3bits (z);

        return xx * 4 + yy * 2 + zz;
    }

    //DECODE
    public static void morton3DDecode (ulong morton, out uint x, out uint y, out uint z) {
        z = morton3D_GetThirdBits (morton);
        y = morton3D_GetThirdBits (morton >> 1);
        x = morton3D_GetThirdBits (morton >> 2);
    }
}