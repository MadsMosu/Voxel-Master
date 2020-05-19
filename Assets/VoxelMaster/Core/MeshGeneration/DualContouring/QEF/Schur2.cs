public static class Schur2 {
    public static void rot01 (SMat3 m, float c, float s) {
        SVD.calcSymmetricGivensCoefficients (m.m00, m.m01, m.m11, c, s);
        float cc = c * c;
        float ss = s * s;
        float mix = 2 * c * s * m.m01;
        m.setSymmetric (cc * m.m00 - mix + ss * m.m11, 0, c * m.m02 - s * m.m12,
            ss * m.m00 + mix + cc * m.m11, s * m.m02 + c * m.m12, m.m22);
    }

    public static void rot02 (SMat3 m, float c, float s) {
        SVD.calcSymmetricGivensCoefficients (m.m00, m.m02, m.m22, c, s);
        float cc = c * c;
        float ss = s * s;
        float mix = 2 * c * s * m.m02;
        m.setSymmetric (cc * m.m00 - mix + ss * m.m22, c * m.m01 - s * m.m12, 0,
            m.m11, s * m.m01 + c * m.m12, ss * m.m00 + mix + cc * m.m22);
    }

    public static void rot12 (SMat3 m, float c, float s) {
        SVD.calcSymmetricGivensCoefficients (m.m11, m.m12, m.m22, c, s);
        float cc = c * c;
        float ss = s * s;
        float mix = 2 * c * s * m.m12;
        m.setSymmetric (m.m00, c * m.m01 - s * m.m02, s * m.m01 + c * m.m02,
            cc * m.m11 - mix + ss * m.m22, 0, ss * m.m11 + mix + cc * m.m22);
    }
}