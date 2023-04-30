using System;
using System.Collections.Generic;
using DrawPanelLibrary;
using System.Drawing;
using System.Collections;
using System.Diagnostics;

namespace Engine3D
{
    class Mesh : List<Triangle>
    {
        public Bitmap texture;
        public bool maxLight = false;

        public bool LoadFromObjectFile(string fileName, Bitmap texture = null)
        {
            this.texture = texture;
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            if (file == System.IO.StreamReader.Null) return false;
            List<Vector> vertices = new List<Vector>();
            List<Vector> textureVerts = new List<Vector>();
            List<Vector> normals = new List<Vector>();
            while (!file.EndOfStream)
            {
                string content = file.ReadLine();
                if (content.Length < 1) continue;
                string[] line = content.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (line[0] == "v") vertices.Add(new Vector(double.Parse(line[1]), double.Parse(line[2]), double.Parse(line[3])));
                else if (line[0] == "vt") textureVerts.Add(new Vector(double.Parse(line[1]), 1 - double.Parse(line[2]), 1));
                //else if (line[0] == "vn") normals.Add(new Vector(double.Parse(line[1]), double.Parse(line[2]), double.Parse(line[3])));
                else if (line[0] == "f")
                {
                    if (line.Length > 5) throw new Exception("File contains faces with more than 4 vertices. Please export model with other options.");
                    Vector av, bv, cv; // vertices
                    Vector at, bt, ct; // textures
                    string[] a = line[1].Split('/');
                    string[] b = line[2].Split('/');
                    string[] c = line[3].Split('/');
                    av = vertices[int.Parse(a[0]) - 1];
                    bv = vertices[int.Parse(b[0]) - 1];
                    cv = vertices[int.Parse(c[0]) - 1];
                    if (a.Length > 1) at = textureVerts[int.Parse(a[1]) - 1]; // note this does not account for negative indices (neg counts from end e.g. -1 would be last verticy)
                    else at = null;
                    if (b.Length > 1) bt = textureVerts[int.Parse(b[1]) - 1];
                    else bt = null;
                    if (c.Length > 1) ct = textureVerts[int.Parse(c[1]) - 1];
                    else ct = null;
                    if (line.Length < 5)
                    {
                        if (at != null && bt != null && ct != null) Add(new Triangle(av, bv, cv, at, bt, ct));
                        else Add(new Triangle(av, bv, cv));
                    }
                    else
                    {
                        string[] d = line[4].Split('/');
                        Vector dv = vertices[int.Parse(d[0]) - 1];
                        Vector dt = null;
                        if (d.Length > 1) dt = textureVerts[int.Parse(d[1]) - 1];
                        if (at != null && bt != null && ct != null && dt != null)
                        {
                            Add(new Triangle(av, bv, cv, at, bt, ct));
                            Add(new Triangle(av, cv, dv, at, ct, dt));
                        }
                        else
                        {
                            Add(new Triangle(av, bv, cv));
                            Add(new Triangle(av, cv, dv));
                        }
                    }
                }
            }
            file.Close();
            return true;
        }

        public void RecalculateNormals()
        {
            foreach (Triangle triangle in this)
            {
                triangle.CalculateNormal();
            }
        }
    }

    class Triangle
    {
        public Vector[] vectors = new Vector[3];
        public Vector normal;
        public Vector luminance;
        public Color color = Color.FromArgb(255, 255, 255);
        public Vector[] texture = new Vector[3];
        public Triangle(Vector a, Vector b, Vector c)
        {
            this[0] = a;
            this[1] = b;
            this[2] = c;
            for (int i = 0; i < 3; i++) texture[i] = new Vector(0, 0, 1);
            //CalculateNormals();
        }

        public Triangle(Vector a, Vector b, Vector c, double au, double av, double bu, double bv, double cu, double cv)
        {
            this[0] = a;
            this[1] = b;
            this[2] = c;
            texture[0] = new Vector(au, av, 1);
            texture[1] = new Vector(bu, bv, 1);
            texture[2] = new Vector(cu, cv, 1);
        }

        public Triangle(Vector a, Vector b, Vector c, Vector normal, double au, double av, double bu, double bv, double cu, double cv)
        {
            this[0] = a;
            this[1] = b;
            this[2] = c;
            texture[0] = new Vector(au, av, 1);
            texture[1] = new Vector(bu, bv, 1);
            texture[2] = new Vector(cu, cv, 1);
            this.normal = normal;
        }

        public Triangle(Vector a, Vector b, Vector c, Vector at, Vector bt, Vector ct)
        {
            this[0] = a;
            this[1] = b;
            this[2] = c;
            texture[0] = at;
            texture[1] = bt;
            texture[2] = ct;
        }

        public Triangle(Vector a, Vector b, Vector c, Vector normal, Vector at, Vector bt, Vector ct)
        {
            this[0] = a;
            this[1] = b;
            this[2] = c;
            texture[0] = at;
            texture[1] = bt;
            texture[2] = ct;
            this.normal = normal;
        }

        public Triangle()
        {
            this[0] = new Vector();
            this[1] = new Vector();
            this[2] = new Vector();
            for (int i = 0; i < 3; i++) texture[i] = new Vector(0, 0, 1);
        }

        public Triangle Copy()
        {
            Triangle result = new Triangle(vectors[0].Copy(), vectors[1].Copy(), vectors[2].Copy());
            if (luminance != null) result.luminance = luminance.Copy();
            if (normal != null) result.normal = normal.Copy();
            if (texture != null) result.texture = new Vector[] { texture[0].Copy(), texture[1].Copy(), texture[2].Copy() };
            return result;
        }

        public void CalculateNormal()
        {
            Vector lineA, lineB;
            lineA = this[1].Subtract(this[0]);
            lineB = this[2].Subtract(this[0]);
            normal = Vector.CrossProduct(lineA, lineB).Normalize();
        }

        public static Triangle FromPoints(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3)
        {
            return new Triangle(new Vector(x1, y1, z1), new Vector(x2, y2, z2), new Vector(x3, y3, z3));
        }

        public static Triangle FromPoints(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3, Color color)
        {
            Triangle result = new Triangle(new Vector(x1, y1, z1), new Vector(x2, y2, z2), new Vector(x3, y3, z3));
            result.color = color;
            return result;
        }

        public static Triangle FromPoints(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3,
            double u1, double v1, double u2, double v2, double u3, double v3)
        {
            return new Triangle(new Vector(x1, y1, z1), new Vector(x2, y2, z2), new Vector(x3, y3, z3), u1, v1, u2, v2, u3, v3);
        }

        public Vector this[int pointIndex]
        {
            get { return vectors[pointIndex]; }
            set { vectors[pointIndex] = value; }
        }

        public PointF[] GetPoints()
        {
            PointF[] points = new PointF[3];
            for (int i = 0; i < 3; i++)
                points[i] = new PointF((float)vectors[i].X, (float)vectors[i].Y);
            return points;
        }

        public Point[] GetInts()
        {
            Point[] points = new Point[3];
            for (int i = 0; i < 3; i++)
                points[i] = new Point((int)Math.Round(vectors[i].X), (int)Math.Round(vectors[i].Y));
            return points;
        }
    }

    class Matrix
    {
        private double[,] data;
        private int row, col;

        public Matrix(int rows, int colums)
        {
            row = rows;
            col = colums;
            data = new double[rows, colums];
            Fill(0);
        }

        public void Fill(double value)
        {
            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                    data[i, j] = value;
        }

        public void Debug()
        {
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                    System.Diagnostics.Debug.Write("|" + data[i, j]);
                System.Diagnostics.Debug.WriteLine("");
            }
        }

        public static Matrix MultiplyMatrix(Matrix A, Matrix B)
        {
            Matrix result = new Matrix(4, 4);
            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 4; r++)
                    result[r, c] = A[r, 0] * B[0, c] + A[r, 1] * B[1, c] + A[r, 2] * B[2, c] + A[r, 3] * B[3, c];
            return result;
        }

        public double this[int row, int column]
        {
            get { return data[row, column]; }
            set { data[row, column] = value; }
        }
    }

    class Vector
    {
        public double X, Y, Z;

        public Vector(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public static Vector IntersectPlane(Vector planePt, Vector normal, Vector lineStart, Vector lineEnd, out double t)
        {
            normal.Normalize();
            double planeD = -DotProduct(normal, planePt);
            double ad = DotProduct(lineStart, normal);
            double bd = DotProduct(lineEnd, normal);
            t = (-planeD - ad) / (bd - ad);
            Vector lineStartToEnd = lineEnd.Subtract(lineStart);
            Vector lineToIntersect = lineStartToEnd.Multiply(t);
            return lineStart.Add(lineToIntersect);
        }

        public static double SignedShortestDist(Vector point, Vector normal, Vector plane)
        {
            //Vector p = point.Copy().Normalize();
            //return normal.X * p.X + normal.Y * p.Y + normal.Z * p.Z - DotProduct(normal, plane);
            return normal.X * point.X + normal.Y * point.Y + normal.Z * point.Z - DotProduct(normal, plane);
        }

        public Vector Subtract(Vector vector)
        {
            return new Vector(X - vector.X, Y - vector.Y, Z - vector.Z);
        }

        public Vector Subtract(double x, double y, double z)
        {
            return new Vector(X - x, Y - y, Z - z);
        }

        public Vector Add(Vector vector)
        {
            return new Vector(X + vector.X, Y + vector.Y, Z + vector.Z);
        }

        public Vector Add(double x, double y, double z)
        {
            return new Vector(X + x, Y + y, Z + z);
        }


        public Vector Multiply(double scalar)
        {
            return new Vector(X * scalar, Y * scalar, Z * scalar);
        }

        public Vector Divide(double scalar)
        {
            return new Vector(X / scalar, Y / scalar, Z / scalar);
        }

        public static Vector CrossProduct(Vector A, Vector B)
        {
            return new Vector(A.Y * B.Z - A.Z * B.Y, A.Z * B.X - A.X * B.Z, A.X * B.Y - A.Y * B.X);
        }

        public static double DotProduct(Vector A, Vector B)
        {
            return A.X * B.X + A.Y * B.Y + A.Z * B.Z;
        }

        public static double Map(double value, double low, double high, double endMin, double endMax)
        {
            return (value - low) / (high - low) * (endMax - endMin) + endMin;
        }

        public Vector Normalize()
        {
            double length = this.Length();
            X /= length;
            Y /= length;
            Z /= length;
            return this;
        }

        public double Length()
        {
            return Math.Sqrt(DotProduct(this, this));
        }

        public Vector Copy()
        {
            return new Vector(X, Y, Z);
        }

        public static Vector Create(double x, double y, double z)
        {
            return new Vector(x, y, z);
        }

        public double this[int index]
        {
            get { return (index == 0) ? X : (index == 1) ? Y : (index == 2) ? Z : 0; }
            set
            {
                X = (index == 0) ? value : X;
                Y = (index == 1) ? value : Y;
                Z = (index == 2) ? value : Z;
            }
        }
    }
}
