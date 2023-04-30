using System;

namespace Engine3D
{
    static class Matrices
    {
        public static Matrix Rotate(double degrees, Vector axis)
        {
            Matrix result = new Matrix(4, 4);
            double radians = degrees * Math.PI / 180;
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            double C = 1 - c;
            result[0, 0] = axis.X * axis.X * C + c;
            result[1, 0] = axis.X * axis.Y * C - axis.Z * s;
            result[2, 0] = axis.X * axis.Z * C + axis.Y * s;
            result[0, 1] = axis.Y * axis.X * C + axis.Z * s;
            result[1, 1] = axis.Y * axis.Y * C + c;
            result[2, 1] = axis.Y * axis.Z * C - axis.X * s;
            result[0, 2] = axis.Z * axis.X * C - axis.Y * s;
            result[1, 2] = axis.Z * axis.Y * C + axis.X * s;
            result[2, 2] = axis.Z * axis.Z * C + c;
            return result;
        }

        public static Matrix RotateRad(double radians, Vector axis)
        {
            Matrix result = new Matrix(4, 4);
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            double C = 1 - c;
            result[0, 0] = axis.X * axis.X * C + c;
            result[1, 0] = axis.X * axis.Y * C - axis.Z * s;
            result[2, 0] = axis.X * axis.Z * C + axis.Y * s;
            result[0, 1] = axis.Y * axis.X * C + axis.Z * s;
            result[1, 1] = axis.Y * axis.Y * C + c;
            result[2, 1] = axis.Y * axis.Z * C - axis.X * s;
            result[0, 2] = axis.Z * axis.X * C - axis.Y * s;
            result[1, 2] = axis.Z * axis.Y * C + axis.X * s;
            result[2, 2] = axis.Z * axis.Z * C + c;
            return result;
        }

        public static Matrix RotateZ(double degrees)
        {
            Matrix result = new Matrix(4, 4);
            double radians = degrees * Math.PI / 180;
            result[0, 0] = Math.Cos(radians);
            result[0, 1] = Math.Sin(radians);
            result[1, 0] = -Math.Sin(radians);
            result[1, 1] = Math.Cos(radians);
            result[2, 2] = 1.0;
            result[3, 3] = 1.0;
            return result;
        }

        public static Matrix RotateZRad(double radians)
        {
            Matrix result = new Matrix(4, 4);
            result[0, 0] = Math.Cos(radians);
            result[0, 1] = Math.Sin(radians);
            result[1, 0] = -Math.Sin(radians);
            result[1, 1] = Math.Cos(radians);
            result[2, 2] = 1.0;
            result[3, 3] = 1.0;
            return result;
        }

        public static Matrix RotateY(double degrees)
        {
            Matrix result = new Matrix(4, 4);
            double radians = degrees * Math.PI / 180;
            result[0, 0] = Math.Cos(radians);
            result[0, 2] = Math.Sin(radians);
            result[2, 0] = -Math.Sin(radians);
            result[2, 2] = Math.Cos(radians);
            result[1, 1] = 1.0;
            result[3, 3] = 1.0;
            return result;
        }

        public static Matrix RotateYRad(double radians)
        {
            Matrix result = new Matrix(4, 4);
            result[0, 0] = Math.Cos(radians);
            result[0, 2] = Math.Sin(radians);
            result[2, 0] = -Math.Sin(radians);
            result[2, 2] = Math.Cos(radians);
            result[1, 1] = 1.0;
            result[3, 3] = 1.0;
            return result;
        }

        public static Matrix RotateX(double degrees)
        {
            Matrix result = new Matrix(4, 4);
            double radians = degrees * Math.PI / 180;
            result[1, 1] = Math.Cos(radians);
            result[1, 2] = Math.Sin(radians);
            result[2, 1] = -Math.Sin(radians);
            result[2, 2] = Math.Cos(radians);
            result[0, 0] = 1.0;
            result[3, 3] = 1.0;
            return result;
        }

        public static Matrix RotateXRad(double radians)
        {
            Matrix result = new Matrix(4, 4);
            result[1, 1] = Math.Cos(radians);
            result[1, 2] = Math.Sin(radians);
            result[2, 1] = -Math.Sin(radians);
            result[2, 2] = Math.Cos(radians);
            result[0, 0] = 1.0;
            result[3, 3] = 1.0;
            return result;
        }

        public static Matrix Translate(double x, double y, double z)
        {
            Matrix result = new Matrix(4, 4);
            result[0, 0] = 1.0;
            result[1, 1] = 1.0;
            result[2, 2] = 1.0;
            result[3, 3] = 1.0;
            result[3, 0] = x;
            result[3, 1] = y;
            result[3, 2] = z;
            return result;
        }

        public static Matrix Translate(Vector vector)
        {
            return Translate(vector.X, vector.Y, vector.Z);
        }

        public static Matrix Identity()
        {
            Matrix result = new Matrix(4, 4);
            result.Fill(1.0);
            return result;
        }

        public static Matrix PointAt(Vector pos, Vector target, Vector up)
        {
            // Calculate new forward direction
            Vector newForward = target.Subtract(pos);
            newForward = newForward.Normalize();

            // Calculate new Up direction
            Vector a = newForward.Multiply(Vector.DotProduct(up, newForward));
            Vector newUp = up.Subtract(a);
            newUp = newUp.Normalize();

            // New Right direction is cross product
            Vector newRight = Vector.CrossProduct(newUp, newForward);

            // Construct Dimensioning and Translation Matrix	
            Matrix result = new Matrix(4, 4);
            result[0, 0] = newRight.X; result[0, 1] = newRight.Y; result[0, 2] = newRight.Z; result[0, 3] = 0.0;
            result[1, 0] = newUp.X; result[1, 1] = newUp.Y; result[1, 2] = newUp.Z; result[1, 3] = 0.0;
            result[2, 0] = newForward.X; result[2, 1] = newForward.Y; result[2, 2] = newForward.Z; result[2, 3] = 0.0;
            result[3, 0] = pos.X; result[3, 1] = pos.Y; result[3, 2] = pos.Z; result[3, 3] = 1.0;
            return result;
        }

        public static Matrix QuickInverse(Matrix matrix) // Only for Rotation/Translation Matrices
        {
            Matrix result = new Matrix(4, 4);
            result[0, 0] = matrix[0, 0]; result[0, 1] = matrix[1, 0]; result[0, 2] = matrix[2, 0]; result[0, 3] = 0.0;
            result[1, 0] = matrix[0, 1]; result[1, 1] = matrix[1, 1]; result[1, 2] = matrix[2, 1]; result[1, 3] = 0.0;
            result[2, 0] = matrix[0, 2]; result[2, 1] = matrix[1, 2]; result[2, 2] = matrix[2, 2]; result[2, 3] = 0.0;
            result[3, 0] = -(matrix[3, 0] * result[0, 0] + matrix[3, 1] * result[1, 0] + matrix[3, 2] * result[2, 0]);
            result[3, 1] = -(matrix[3, 0] * result[0, 1] + matrix[3, 1] * result[1, 1] + matrix[3, 2] * result[2, 1]);
            result[3, 2] = -(matrix[3, 0] * result[0, 2] + matrix[3, 1] * result[1, 2] + matrix[3, 2] * result[2, 2]);
            result[3, 3] = 1.0;
            return result;
        }
    }
}
