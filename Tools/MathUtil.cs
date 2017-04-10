using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Tools
{
    public class MathUtil
    {
        public static Matrix T2Quaternion(Matrix<double> T)
        {
            Matrix Q = DenseMatrix.Create(1, 4, 0);

            double Q00 = Complex32.Divide(Complex32.Sqrt((float)(T[0, 0] + T[1, 1] + T[2, 2] + T[3, 3])), 2).Real;
            // double Q00 = Complex.Sqrt((T[0, 0] + T[1, 1] + T[2, 2] + T[3, 3]) / 2.0).Real;
            Q[0, 0] = Q00;
            double Lx = T[2, 1] - T[1, 2];
            double Ly = T[0, 2] - T[2, 0];
            double Lz = T[1, 0] - T[0, 1];
            double Lx1, Ly1, Lz1;

            if (T[0, 0] >= T[1, 1] && T[0, 0] >= T[2, 2])
            {
                Lx1 = T[0, 0] - T[1, 1] - T[2, 2] + 1;
                Ly1 = T[1, 0] + T[0, 1];
                Lz1 = T[2, 0] + T[0, 2];
            }
            else if (T[1, 1] >= T[2, 2])
            {
                Lx1 = T[1, 0] + T[0, 1];
                Ly1 = T[1, 1] - T[0, 0] - T[2, 2] + 1;
                Lz1 = T[2, 1] + T[1, 2];
            }
            else
            {
                Lx1 = T[2, 0] + T[0, 2];
                Ly1 = T[2, 1] + T[1, 2];
                Lz1 = T[2, 2] - T[0, 0] - T[1, 1] + 1;
            }

            if (Lx >= 0 || Ly >= 0 || Lz >= 0)
            {
                Lx = Lx + Lx1;
                Ly = Ly + Ly1;
                Lz = Lz + Lz1;
            }
            else
            {
                Lx = Lx - Lx1;
                Ly = Ly - Ly1;
                Lz = Lz - Lz1;
            }

            Vector<double> v = new DenseVector(new double[] { Lx, Ly, Lz });
            if (v.L2Norm() == 0)
            {
                Q[0, 0] = 1; Q[0, 1] = 0; Q[0, 1] = 0; Q[0, 2] = 0; Q[0, 3] = 0;
            }
            else
            {
                double s = Math.Sqrt(1 - Q[0, 0] * Q[0, 0]) / v.L2Norm();
                Q[0, 1] = s * Lx; Q[0, 2] = s * Ly; Q[0, 3] = s * Lz;
            }

            return Q;
        }

        public static Matrix Quaternion2T(Matrix Q, Vector p) //Q are quaternions, describing orientation, p is position
        {

            // Ensure Q has unit norm
            double Q00 = Q[0, 0];
            double Q01 = Q[0, 1];
            double Q02 = Q[0, 2];
            double Q03 = Q[0, 3];
            double QNorm = Math.Sqrt(Q00 * Q00 + Q01 * Q01 + Q02 * Q02 + Q03 * Q03);

            if (QNorm != 0)
            {
                Q[0, 0] = Q[0, 0] / QNorm;
                Q[0, 1] = Q[0, 1] / QNorm;
                Q[0, 2] = Q[0, 2] / QNorm;
                Q[0, 3] = Q[0, 3] / QNorm;
            }

            // Set up convenience variables
            double w = Q[0, 0]; double x = Q[0, 1]; double y = Q[0, 2]; double z = Q[0, 3];
            double w2 = w * w; double x2 = x * x; double y2 = y * y; double z2 = z * z;
            double xy = x * y; double xz = x * z; double yz = y * z;
            double wx = w * x; double wy = w * y; double wz = w * z;

            Matrix T = DenseMatrix.Create(4, 4, 0);
            T[0, 0] = w2 + x2 - y2 - z2; T[0, 1] = 2 * (xy - wz); T[0, 2] = 2 * (wy + xz); T[0, 3] = p[0];
            T[1, 0] = 2 * (wz + xy); T[1, 1] = w2 - x2 + y2 - z2; T[1, 2] = 2 * (yz - wx); T[1, 3] = p[1];
            T[2, 0] = 2 * (xz - wy); T[2, 1] = 2 * (wx + yz); T[2, 2] = w2 - x2 - y2 + z2; T[2, 3] = p[2];
            T[3, 0] = 0; T[3, 1] = 0; T[3, 2] = 0; T[3, 3] = 1;

            return T;
        }

        public static EulerAngles Quaternion2EulerAngles(DenseVector v)
        {
            Quaternion quat = new Quaternion(v);
            return quat.ToEulerAngles();
        }

    }
}
