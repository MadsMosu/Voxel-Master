public static class Givens {
    public static void rot01_post (Mat3 m, float c, float s) {
        float m00 = m.m00, m01 = m.m01, m10 = m.m10, m11 = m.m11, m20 = m.m20, m21 = m.m21;
        m.set (c * m00 - s * m01, s * m00 + c * m01, m.m02, c * m10 - s * m11,
            s * m10 + c * m11, m.m12, c * m20 - s * m21, s * m20 + c * m21, m.m22);
    }

    public static void rot02_post (Mat3 m, float c, float s) {
        float m00 = m.m00, m02 = m.m02, m10 = m.m10, m12 = m.m12, m20 = m.m20, m22 = m.m22;
        m.set (c * m00 - s * m02, m.m01, s * m00 + c * m02, c * m10 - s * m12, m.m11,
            s * m10 + c * m12, c * m20 - s * m22, m.m21, s * m20 + c * m22);
    }

    public static void rot12_post (Mat3 m, float c, float s) {
        float m01 = m.m01, m02 = m.m02, m11 = m.m11, m12 = m.m12, m21 = m.m21, m22 = m.m22;
        m.set (m.m00, c * m01 - s * m02, s * m01 + c * m02, m.m10, c * m11 - s * m12,
            s * m11 + c * m12, m.m20, c * m21 - s * m22, s * m21 + c * m22);
    }
}