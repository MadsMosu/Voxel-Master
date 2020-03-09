using System;
using UnityEngine;

public class QefSolver {
    private QefData data;
    private SMat3 ata;
    private Vector3 atb, massPoint, x;
    private bool hasSolution;

    public QefSolver () {
        data = new QefData ();
        ata = new SMat3 ();
        atb = Vector3.zero;
        massPoint = Vector3.zero;
        x = Vector3.zero;
        hasSolution = false;
    }

    private QefSolver (QefSolver rhs) { }

    public Vector3 getMassPoint () {
        return massPoint;
    }

    public void add (float px, float py, float pz, float nx, float ny, float nz) {
        hasSolution = false;

        Vector3 tmpv = new Vector3 (nx, ny, nz).normalized;
        nx = tmpv.x;
        ny = tmpv.y;
        nz = tmpv.z;

        data.ata_00 += nx * nx;
        data.ata_01 += nx * ny;
        data.ata_02 += nx * nz;
        data.ata_11 += ny * ny;
        data.ata_12 += ny * nz;
        data.ata_22 += nz * nz;
        float dot = nx * px + ny * py + nz * pz;
        data.atb_x += dot * nx;
        data.atb_y += dot * ny;
        data.atb_z += dot * nz;
        data.btb += dot * dot;
        data.massPoint_x += px;
        data.massPoint_y += py;
        data.massPoint_z += pz;
        ++data.numPoints;
    }

    public void add (Vector3 p, Vector3 n) {
        add (p.x, p.y, p.z, n.x, n.y, n.z);
    }

    public void add (QefData rhs) {
        hasSolution = false;
        data.add (rhs);
    }

    public QefData getData () {
        return data;
    }

    public float getError () {
        if (!hasSolution) {
            throw new ArgumentException ("Qef Solver does not have a solution!");
        }

        return getError (x);
    }

    public float getError (Vector3 pos) {
        if (!hasSolution) {
            setAta ();
            setAtb ();
        }

        Vector3 atax;
        MatUtils.vmul_symmetric (out atax, ata, pos);
        return Vector3.Dot (pos, atax) - 2 * Vector3.Dot (pos, atb) + data.btb;
    }

    public void reset () {
        hasSolution = false;
        data.clear ();
    }

    public float solve (Vector3 outx, float svd_tol, int svd_sweeps, float pinv_tol) {
        if (data.numPoints == 0) {
            throw new ArgumentException ("...");
        }

        massPoint.Set (data.massPoint_x, data.massPoint_y, data.massPoint_z);
        massPoint *= (1.0f / data.numPoints);
        setAta ();
        setAtb ();
        Vector3 tmpv;
        MatUtils.vmul_symmetric (out tmpv, ata, massPoint);
        atb = atb - tmpv;
        x = Vector3.zero;
        float result = SVD.solveSymmetric (ata, atb, x, svd_tol, svd_sweeps, pinv_tol);
        x += massPoint * 1;
        setAtb ();
        outx = x;
        hasSolution = true;
        return result;
    }

    private void setAta () {
        ata.setSymmetric (data.ata_00, data.ata_01, data.ata_02, data.ata_11, data.ata_12, data.ata_22);
    }

    private void setAtb () {
        atb.Set (data.atb_x, data.atb_y, data.atb_z);
    }

}