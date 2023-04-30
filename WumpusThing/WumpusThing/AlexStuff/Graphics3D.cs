using System;
using System.Collections.Generic;
using DrawPanelLibrary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Engine3D
{
    class Graphics3D
    {
        Bitmap rendered;
        public Graphics graphics;

        Matrix projection;
        Matrix camera;
        Vector lightPosition = new Vector(0, 700, 0);
        double[,] depthBuffer;

        object[] lockBuffer;

        public Graphics3D(int width, int height)
        {
            depthBuffer = new double[width, height];
            rendered = new Bitmap(width, height);
            graphics = Graphics.FromImage(rendered);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            SetProjection(width / height);
            SetCamera();
            lockBuffer = new object[width * height];
            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        public Bitmap GetRender()
        {
            return this.rendered;
        }

        public void DrawRender(Graphics g)
        {
            g.DrawImage(this.rendered, 0, 0);
        }

        /// <summary>
        /// Gouraud Shading only works if mesh has a texture.
        /// </summary>
        public void RenderMesh(Mesh mesh, Vector worldPos = null, Vector rotation = null)
        {
            if (worldPos == null) worldPos = new Vector();
            if (rotation == null) rotation = new Vector();
            Matrix world = WorldTransform(worldPos, rotation);
            ProjectSmall(mesh, world);
        }

        public void RenderMeshRad(Mesh mesh, Vector worldPos = null, Vector rotation = null)
        {
            if (worldPos == null) worldPos = new Vector();
            if (rotation == null) rotation = new Vector();
            Matrix world = WorldTransformRad(worldPos, rotation);
            ProjectSmall(mesh, world);
        }

        public void RenderSkyboxRad(Mesh mesh, Vector worldPos = null, Vector rotation = null)
        {
            if (worldPos == null) worldPos = new Vector();
            if (rotation == null) rotation = new Vector();
            Matrix world = WorldTransformRad(worldPos, rotation);
            ProjectBig(mesh, world);
        }

        public void RenderMesh(Mesh mesh, Matrix worldTransform)
        {
            ProjectSmall(mesh, worldTransform);
        }

        public Vector ProjectPosition(Vector point)
        {
            Vector result = ApplyMatrix(point, camera);
            double nearPlaneDistance = Vector.SignedShortestDist(result, new Vector(0.0, 0.0, -1), new Vector(0.0, 0.0, -0.1));
            //double planeDistance1 = Vector.SignedShortestDist(result, new Vector(0, 1, 0), new Vector(0, 0, 0));
            //double planeDistance2 = Vector.SignedShortestDist(result, new Vector(0, -1, 0), new Vector(0, rendered.Height, 0));
            //double planeDistance3 = Vector.SignedShortestDist(result, new Vector(1, 0, 0), new Vector(0, 0, 0));
            //double planeDistance4 = Vector.SignedShortestDist(result, new Vector(-1, 0, 0), new Vector(rendered.Width, 0, 0));
            //bool isVisible = nearPlaneDistance >= 0 && planeDistance1 >= 0 && planeDistance2 >= 0 && planeDistance3 >= 0 && planeDistance4 >= 0;
            //if (!isVisible) return null;
            if (nearPlaneDistance < 0) return null;
            result = ApplyMatrix(result, projection);
            double scale = 0.5 * rendered.Width;
            Vector vOffsetView = new Vector(1, 1, 0);
            result = result.Add(vOffsetView);
            result.X *= scale; result.Y *= scale;
            return result;
        }

        public bool IsVisible(Vector point, double sizeRadius)
        {
            Vector projected = ApplyMatrix(point, camera);
            //double nearPlaneDistance = Vector.SignedShortestDist(projected, new Vector(0.0, 0.0, -1), new Vector(0.0, 0.0, sizeRadius));
            //if (nearPlaneDistance < 0) return false;
            if (projected.Z > sizeRadius) return false;
            Vector bottomRight = ApplyMatrix(projected.Subtract(sizeRadius, sizeRadius, 0), projection);
            Vector topLeft = ApplyMatrix(projected.Add(sizeRadius, sizeRadius, 0), projection);
            double scale = rendered.Width * 0.5;
            topLeft.X = (topLeft.X + 1) * scale;
            topLeft.Y = (topLeft.Y + 1) * scale;
            bottomRight.X = (bottomRight.X + 1) * scale;
            bottomRight.Y = (bottomRight.Y + 1) * scale;
            return !(bottomRight.X < 0 || bottomRight.Y < 0 || topLeft.X > rendered.Width || topLeft.Y > rendered.Height);
            }

        private unsafe void ProjectBig(Mesh triangles, Matrix transform)
        {
            int textureWidth = triangles.texture.Width;
            int textureHeight = triangles.texture.Height;
            int width = rendered.Width, height = rendered.Height;
            Vector lightPosition = this.lightPosition;
            double scale = 0.5 * width;
            Vector vOffsetView = new Vector(1, 1, 0);
            BitmapData renderData = rendered.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData textureData = triangles.texture.LockBits(new Rectangle(0, 0, textureWidth, textureHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;
            byte* scan0 = (byte*)renderData.Scan0.ToPointer();
            int stride = renderData.Stride;
            byte* tscan0 = (byte*)textureData.Scan0.ToPointer();
            int tstride = textureData.Stride;
            bool maxLight = triangles.maxLight;
            foreach (Triangle triangle in triangles)
            {
                RenderTriangle(transform, textureWidth, textureHeight, width, height, lightPosition, scale, vOffsetView, renderData, textureData, bytesPerPixel, scan0, stride, tscan0, tstride, maxLight, triangle, false);
            }
            rendered.UnlockBits(renderData);
            triangles.texture.UnlockBits(textureData);
        }

        private unsafe void ProjectSmall(Mesh triangles, Matrix transform)
        {
            int textureWidth = triangles.texture.Width;
            int textureHeight = triangles.texture.Height;
            int width = rendered.Width, height = rendered.Height;
            Vector lightPosition = this.lightPosition;
            double scale = 0.5 * width;
            Vector vOffsetView = new Vector(1, 1, 0);
            BitmapData renderData = rendered.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData textureData = triangles.texture.LockBits(new Rectangle(0, 0, textureWidth, textureHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;
            byte* scan0 = (byte*)renderData.Scan0.ToPointer();
            int stride = renderData.Stride;
            byte* tscan0 = (byte*)textureData.Scan0.ToPointer();
            int tstride = textureData.Stride;
            bool maxLight = triangles.maxLight;
            Parallel.ForEach(triangles, triangle =>
            {
                RenderTriangle(transform, textureWidth, textureHeight, width, height, lightPosition, scale, vOffsetView, renderData, textureData, bytesPerPixel, scan0, stride, tscan0, tstride, maxLight, triangle, true);
            });
            rendered.UnlockBits(renderData);
            triangles.texture.UnlockBits(textureData);
        }

        private unsafe void RenderTriangle(Matrix transform, int textureWidth, int textureHeight, int width, int height, Vector lightPosition, double scale, Vector vOffsetView, BitmapData renderData, BitmapData textureData, int bytesPerPixel, byte* scan0, int stride, byte* tscan0, int tstride, bool maxLight, Triangle triangle, bool parallel)
        {
            Triangle projected = triangle.Copy();
            projected[0] = ApplyMatrix(projected[0], transform);
            projected[1] = ApplyMatrix(projected[1], transform);
            projected[2] = ApplyMatrix(projected[2], transform);

            // Gouraud Shading (lighting with interpolation btwn verticy normals)
            if (projected.normal == null) projected.normal = CalculateNormal(projected);
            projected.luminance = new Vector(
                Vector.Map(Vector.DotProduct(projected.normal, lightPosition.Subtract(projected[0]).Normalize()), -1, 1, 0.1, 1),
                Vector.Map(Vector.DotProduct(projected.normal, lightPosition.Subtract(projected[1]).Normalize()), -1, 1, 0.1, 1),
                Vector.Map(Vector.DotProduct(projected.normal, lightPosition.Subtract(projected[2]).Normalize()), -1, 1, 0.1, 1));

            projected[0] = ApplyMatrix(projected[0], camera);
            projected[1] = ApplyMatrix(projected[1], camera);
            projected[2] = ApplyMatrix(projected[2], camera);

            // only display triangles w/ normal facing camera
            if (Vector.DotProduct(CalculateNormal(projected), projected[0]) < 0)
            {
                // clip against near plane
                Triangle[] clipped = new Triangle[2]; //                          \/ znear
                int clippedCount = TriangleClipAgainstPlane(new Vector(0.0, 0.0, -0.1), new Vector(0.0, 0.0, -1), projected, out clipped[0], out clipped[1]);

                if (clippedCount > 0)
                {
                    double w;
                    for (int i = 0; i < 3; i++)
                    {
                        clipped[0][i] = ApplyMatrix(clipped[0][i], projection, out w);
                        clipped[0][i] = clipped[0][i].Add(vOffsetView);
                        clipped[0].texture[i].X /= -w;
                        clipped[0].texture[i].Y /= -w;
                        clipped[0].texture[i].Z /= -w;
                        clipped[0][i].X *= scale; clipped[0][i].Y *= scale;
                    }
                    List<Triangle> clip = ClipWithScreenEdges(clipped[0], width, height);
                    //if (!IsHidden(clipped[0], width, height)) RasterizeTriangle(clipped[0], 0.5, bytesPerPixel, scan0,
                    //    stride, tscan0, tstride, width, height, textureWidth, textureHeight,
                    //    maxLight, parallel);
                    if (clippedCount > 1)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            clipped[1][i] = ApplyMatrix(clipped[1][i], projection, out w);
                            clipped[1][i] = clipped[1][i].Add(vOffsetView);
                            clipped[1].texture[i].X /= -w;
                            clipped[1].texture[i].Y /= -w;
                            clipped[1].texture[i].Z /= -w;
                            clipped[1][i].X *= scale; clipped[1][i].Y *= scale;
                        }
                        //if (!IsHidden(clipped[1], width, height)) RasterizeTriangle(clipped[1], 0.5, bytesPerPixel, scan0,
                        //    stride, tscan0, tstride, width, height, textureWidth, textureHeight,
                        //    maxLight, parallel);
                        clip.AddRange(ClipWithScreenEdges(clipped[1], width, height));
                    }
                    foreach (Triangle tri in clip)
                        RasterizeTriangle(tri, 0.5, bytesPerPixel, scan0,
                            stride, tscan0, tstride, width, height, textureWidth, textureHeight,
                            maxLight, parallel);
                }
            }
        }

        private bool IsHidden(Triangle projected, int width, int height)
        {
            return (projected[0].X < 0 || projected[0].Y < 0 || projected[0].X > width || projected[0].Y > height) &&
                (projected[1].X < 0 || projected[1].Y < 0 || projected[1].X > width || projected[1].Y > height) &&
                (projected[2].X < 0 || projected[2].Y < 0 || projected[2].X > width || projected[2].Y > height);
        }

        private unsafe void RasterizeTriangle(Triangle triangle, double IsZero, int bytesPerPixel, byte* scan0,
            int stride, byte* tscan0, int tstride, int renderedWidth, int renderedHeight, int textureWidth, int textureHeight,
            bool maxLight, bool parallel)
        {
            Point[] ints = triangle.GetInts();
            Point a = ints[0], b = ints[1], c = ints[2];
            Vector at = triangle.texture[0], bt = triangle.texture[1], ct = triangle.texture[2];
            Vector lum = triangle.luminance;
            double al = lum.X, bl = lum.Y, cl = lum.Z;
            if (b.Y < a.Y) { Swap(ref a, ref b); Swap(ref at, ref bt); Swap(ref al, ref bl); }
            if (c.Y < a.Y) { Swap(ref a, ref c); Swap(ref at, ref ct); Swap(ref al, ref cl); }
            if (c.Y < b.Y) { Swap(ref b, ref c); Swap(ref bt, ref ct); Swap(ref bl, ref cl); }

            double dy1 = b.Y - a.Y;
            double dx1 = b.X - a.X;
            double dv1 = bt.Y - at.Y;
            double du1 = bt.X - at.X;
            double dw1 = bt.Z - at.Z;
            double dl1 = bl - al;

            double dy2 = c.Y - a.Y;
            double dx2 = c.X - a.X;
            double dv2 = ct.Y - at.Y;
            double du2 = ct.X - at.X;
            double dw2 = ct.Z - at.Z;
            double dl2 = cl - al;

            //double tex_u, tex_v, tex_w;
            //double luminance;

            double dax_step = 0, dbx_step = 0,
                du1_step = 0, dv1_step = 0,
                du2_step = 0, dv2_step = 0,
                dw1_step = 0, dw2_step = 0,
                dl1_step = 0, dl2_step = 0;

            if (Math.Abs(dy1) > IsZero) dax_step = dx1 / Math.Abs(dy1);
            if (Math.Abs(dy2) > IsZero) dbx_step = dx2 / Math.Abs(dy2);

            if (Math.Abs(dy1) > IsZero) du1_step = du1 / Math.Abs(dy1);
            if (Math.Abs(dy1) > IsZero) dv1_step = dv1 / Math.Abs(dy1);
            if (Math.Abs(dy1) > IsZero) dw1_step = dw1 / Math.Abs(dy1);

            if (Math.Abs(dy2) > IsZero) du2_step = du2 / Math.Abs(dy2);
            if (Math.Abs(dy2) > IsZero) dv2_step = dv2 / Math.Abs(dy2);
            if (Math.Abs(dy2) > IsZero) dw2_step = dw2 / Math.Abs(dy2);

            if (Math.Abs(dy1) > IsZero) dl1_step = dl1 / Math.Abs(dy1);
            if (Math.Abs(dy2) > IsZero) dl2_step = dl2 / Math.Abs(dy2);

            if (Math.Abs(dy1) > IsZero)
            {
                int max = (b.Y < renderedHeight) ? b.Y : renderedHeight;
                int min = (a.Y > 0) ? a.Y : 0;
                if (parallel) 
                    for (int i = min; i < max; i++) 
                        RasterizeTop(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, i, ref a, at, al, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                else
                    //{
                    //    max -= 1;
                    //    int halfway = (max + min) / 2;
                    //    bool even = (max + min) % 2 == 0;
                    //    //if (Math.Floor(half) != half) RasterizeTop(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, halfWay + 1, ref a, at, al, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                    //    Parallel.For(min, halfway + 1, i =>
                    //    {
                    //        RasterizeTop(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, i, ref a, at, al, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                    //        if (even && i != halfway) RasterizeTop(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, max + min - i, ref a, at, al, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                    //    });
                    //}
                    Parallel.For(min, max, i =>
                    {
                        RasterizeTop(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, i, ref a, at, al, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                    });
            }
            // triangle part with flat side up
            dy1 = c.Y - b.Y;
            dx1 = c.X - b.X;
            dv1 = ct.Y - bt.Y;
            du1 = ct.X - bt.X;
            dw1 = ct.Z - bt.Z;
            dl1 = cl - bl;

            if (Math.Abs(dy1) > IsZero) dax_step = dx1 / Math.Abs(dy1);
            if (Math.Abs(dy2) > IsZero) dbx_step = dx2 / Math.Abs(dy2);

            du1_step = 0; dv1_step = 0;
            if (Math.Abs(dy1) > IsZero) du1_step = du1 / Math.Abs(dy1);
            if (Math.Abs(dy1) > IsZero) dv1_step = dv1 / Math.Abs(dy1);
            if (Math.Abs(dy1) > IsZero) dw1_step = dw1 / Math.Abs(dy1);

            dl1_step = 0;
            if (Math.Abs(dy1) > IsZero) dl1_step = dl1 / Math.Abs(dy1);

            if (Math.Abs(dy1) > IsZero)
            {
                int max = (c.Y < renderedHeight) ? c.Y : renderedHeight;
                int min = (b.Y > 0) ? b.Y : 0;
                if (parallel) 
                    for (int i = min; i < max; i++) 
                        RasterizeBottom(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, i, a, b, at, bt, al, bl, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                else
                    Parallel.For(min, max, i =>
                    {
                        RasterizeBottom(bytesPerPixel, scan0, stride, tscan0, tstride, renderedWidth, textureWidth, textureHeight, maxLight, i, a, b, at, bt, al, bl, dax_step, dbx_step, du1_step, dv1_step, du2_step, dv2_step, dw1_step, dw2_step, dl1_step, dl2_step);
                    });
            }
        }

        private unsafe void RasterizeBottom(int bytesPerPixel, byte* scan0, int stride, byte* tscan0, int tstride, int renderedWidth, int textureWidth, int textureHeight, bool maxLight, int i, Point a, Point b, Vector at, Vector bt, double al, double bl, double dax_step, double dbx_step, double du1_step, double dv1_step, double du2_step, double dv2_step, double dw1_step, double dw2_step, double dl1_step, double dl2_step)
        {
            double tex_u, tex_v, tex_w;
            double luminance;
            int ax = (int)Math.Round(b.X + (i - b.Y) * dax_step);
            int bx = (int)Math.Round(a.X + (i - a.Y) * dbx_step);

            double tex_su = bt.X + (i - b.Y) * du1_step;
            double tex_sv = bt.Y + (i - b.Y) * dv1_step;
            double tex_sw = bt.Z + (i - b.Y) * dw1_step;

            double tex_eu = at.X + (i - a.Y) * du2_step;
            double tex_ev = at.Y + (i - a.Y) * dv2_step;
            double tex_ew = at.Z + (i - a.Y) * dw2_step;

            double lum_s = bl + (i - b.Y) * dl1_step;
            double lum_e = al + (i - a.Y) * dl2_step;

            if (ax > bx)
            {
                Swap(ref ax, ref bx);
                Swap(ref tex_su, ref tex_eu);
                Swap(ref tex_sv, ref tex_ev);
                Swap(ref tex_sw, ref tex_ew);
                Swap(ref lum_s, ref lum_e);
            }

            //if (ax >= renderedWidth) continue;
            //if (bx < 0) continue;
            if (ax >= renderedWidth) return;
            if (bx < 0) return;

            double tstep = 1.0 / (bx - ax);
            double t = 0.0;
            if (ax < 0) t += tstep * Math.Ceiling((double)-ax);

            for (int j = ((ax < 0) ? 0 : ax); j < ((bx >= renderedWidth) ? renderedWidth : bx); j++)
            {
                tex_u = (1.0 - t) * tex_su + t * tex_eu;
                tex_v = (1.0 - t) * tex_sv + t * tex_ev;
                tex_w = (1.0 - t) * tex_sw + t * tex_ew;
                if (maxLight) luminance = 1;
                else luminance = (1.0 - t) * lum_s + t * lum_e;
                byte* row = scan0 + (i * stride);
                byte* trow = tscan0 + (int)Math.Round((tex_v / tex_w) * (textureHeight - 1) % textureHeight) * tstride;
                int bIndex = (int)Math.Round(tex_u / tex_w * (textureWidth - 1) % textureWidth) * bytesPerPixel;
                int gIndex = bIndex + 1;
                int rIndex = bIndex + 2;
                lock (lockBuffer[i * renderedWidth + j])
                {
                    if (tex_w > depthBuffer[j, i])
                    {
                        byte pixelR = trow[rIndex];
                        byte pixelG = trow[gIndex];
                        byte pixelB = trow[bIndex];
                        bIndex = j * bytesPerPixel;
                        gIndex = bIndex + 1;
                        rIndex = bIndex + 2;
                        row[rIndex] = (byte)Math.Round(pixelR * luminance);
                        row[bIndex] = (byte)Math.Round(pixelB * luminance);
                        row[gIndex] = (byte)Math.Round(pixelG * luminance);
                        depthBuffer[j, i] = tex_w;
                    }
                }
                t += tstep;
            }
        }

        private unsafe void RasterizeTop(int bytesPerPixel, byte* scan0, int stride, byte* tscan0, int tstride, int renderedWidth, int textureWidth, int textureHeight, bool maxLight, int i, ref Point a, Vector at, double al, double dax_step, double dbx_step, double du1_step, double dv1_step, double du2_step, double dv2_step, double dw1_step, double dw2_step, double dl1_step, double dl2_step)
        {
            double tex_u, tex_v, tex_w;
            double luminance;
            int ax = (int)Math.Round(a.X + (i - a.Y) * dax_step);
            int bx = (int)Math.Round(a.X + (i - a.Y) * dbx_step);

            double tex_su = at.X + (i - a.Y) * du1_step;
            double tex_sv = at.Y + (i - a.Y) * dv1_step;
            double tex_sw = at.Z + (i - a.Y) * dw1_step;

            double tex_eu = at.X + (i - a.Y) * du2_step;
            double tex_ev = at.Y + (i - a.Y) * dv2_step;
            double tex_ew = at.Z + (i - a.Y) * dw2_step;

            double lum_s = al + (i - a.Y) * dl1_step;
            double lum_e = al + (i - a.Y) * dl2_step;

            if (ax > bx)
            {
                Swap(ref ax, ref bx);
                Swap(ref tex_su, ref tex_eu);
                Swap(ref tex_sv, ref tex_ev);
                Swap(ref tex_sw, ref tex_ew);
                Swap(ref lum_s, ref lum_e);
            }

            //if (ax >= renderedWidth) continue;
            //if (bx < 0) continue;
            if (ax >= renderedWidth) return;
            if (bx < 0) return;

            double tstep = 1.0 / (bx - ax);
            double t = 0.0;
            if (ax < 0) t += tstep * Math.Ceiling((double)-ax);

            for (int j = ((ax < 0) ? 0 : ax); j < ((bx >= renderedWidth) ? renderedWidth : bx); j++)
            {
                tex_u = (1.0 - t) * tex_su + t * tex_eu;
                tex_v = (1.0 - t) * tex_sv + t * tex_ev;
                tex_w = (1.0 - t) * tex_sw + t * tex_ew;
                if (maxLight) luminance = 1;
                else luminance = (1.0 - t) * lum_s + t * lum_e;
                byte* row = scan0 + (i * stride);
                byte* trow = tscan0 + (int)Math.Round((tex_v / tex_w) * (textureHeight - 1) % textureHeight) * tstride;
                int bIndex = (int)Math.Round(tex_u / tex_w * (textureWidth - 1) % textureWidth) * bytesPerPixel;
                int gIndex = bIndex + 1;
                int rIndex = bIndex + 2;
                lock (lockBuffer[i * renderedWidth + j])
                {
                    if (tex_w > depthBuffer[j, i])
                    {
                        byte pixelR = trow[rIndex];
                        byte pixelG = trow[gIndex];
                        byte pixelB = trow[bIndex];
                        bIndex = j * bytesPerPixel;
                        gIndex = bIndex + 1;
                        rIndex = bIndex + 2;
                        row[rIndex] = (byte)Math.Round(pixelR * luminance);
                        row[bIndex] = (byte)Math.Round(pixelB * luminance);
                        row[gIndex] = (byte)Math.Round(pixelG * luminance);
                        depthBuffer[j, i] = tex_w;
                    }
                }
                t += tstep;
            }
        }

        private void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        private List<Triangle> ClipWithScreenEdges(Triangle triangle, int width, int height)
        {
            Triangle[] clipped = new Triangle[2];
            List<Triangle> listTriangles = new List<Triangle>();

            // Add initial triangle
            listTriangles.Add(triangle);
            int nNewTriangles = 1;

            //int height = rendered.Height;
            //int width = rendered.Width;
            for (int p = 0; p < 4; p++)
            {
                int nTrisToAdd = 0;
                while (nNewTriangles > 0)
                {
                    // Take triangle from front of queue
                    Triangle test = listTriangles[0];
                    listTriangles.RemoveAt(0);
                    nNewTriangles--;

                    // Clip it against a plane
                    switch (p)
                    {
                        case 0:
                            nTrisToAdd = TriangleClipAgainstPlane(new Vector(0, 0, 0), new Vector(0, 1, 0), test, out clipped[0], out clipped[1]);
                            break;
                        case 1:
                            nTrisToAdd = TriangleClipAgainstPlane(new Vector(0, height, 0), new Vector(0, -1, 0), test, out clipped[0], out clipped[1]);
                            break;
                        case 2:
                            nTrisToAdd = TriangleClipAgainstPlane(new Vector(0, 0, 0), new Vector(1, 0, 0), test, out clipped[0], out clipped[1]);
                            break;
                        case 3:
                            nTrisToAdd = TriangleClipAgainstPlane(new Vector(width, 0, 0), new Vector(-1, 0, 0), test, out clipped[0], out clipped[1]);
                            break;
                    }

                    // Clipping may yield a variable number of triangles, so
                    // add these new ones to the back of the queue for subsequent
                    // clipping against next planes
                    for (int w = 0; w < nTrisToAdd; w++)
                        listTriangles.Add(clipped[w]);
                }
                nNewTriangles = listTriangles.Count;
            }
            return listTriangles;
        }

        // returns an int indicating how many triangles are generated in out1 and possibly out2
        private int TriangleClipAgainstPlane(Vector plane, Vector normal, Triangle triangle, out Triangle out1, out Triangle out2)
        {
            normal.Normalize();

            Vector[] outsidePoints = new Vector[3], insidePoints = new Vector[3];
            int inside = 0, outside = 0;
            Vector[] outsideTexture = new Vector[3], insideTexture = new Vector[3];
            Vector insideLum = new Vector(), outsideLum = new Vector();
            Vector lum = triangle.luminance;

            double d0 = Vector.SignedShortestDist(triangle[0], normal, plane);
            double d1 = Vector.SignedShortestDist(triangle[1], normal, plane);
            double d2 = Vector.SignedShortestDist(triangle[2], normal, plane);

            if (d0 >= 0) { insidePoints[inside] = triangle[0]; insideTexture[inside] = triangle.texture[0]; insideLum[inside] = lum.X; inside++; }
            else { outsidePoints[outside] = triangle[0]; outsideTexture[outside] = triangle.texture[0]; outsideLum[outside] = lum.X; outside++; }
            if (d1 >= 0) { insidePoints[inside] = triangle[1]; insideTexture[inside] = triangle.texture[1]; insideLum[inside] = lum.Y; inside++; }
            else { outsidePoints[outside] = triangle[1]; outsideTexture[outside] = triangle.texture[1]; outsideLum[outside] = lum.Y; outside++; }
            if (d2 >= 0) { insidePoints[inside] = triangle[2]; insideTexture[inside] = triangle.texture[2]; insideLum[inside] = lum.Z; inside++; }
            else { outsidePoints[outside] = triangle[2]; outsideTexture[outside] = triangle.texture[2]; outsideLum[outside] = lum.Z; outside++; }

            if (inside == 0) // all points lie on outside (clip entire triangle)
            {
                out1 = null;
                out2 = null;
                return 0;
            }
            else if (inside == 3) // all points lie inside (keep triangle)
            {
                out1 = triangle;
                out2 = null;
                return 1;
            }
            else if (inside == 1 && outside == 2) // only one inside point (triangle becomes smaller)
            {
                out1 = new Triangle();
                out1.texture = new Vector[3];
                out1.luminance = new Vector();
                out1.luminance.X = insideLum.X;
                out1.color = triangle.color;
                //out1.color = Color.Red; // debugging purposes
                out1[0] = insidePoints[0]; // keep inside point
                out1.texture[0] = insideTexture[0].Copy();
                // two new points = intersects of original sides and plane
                double t;
                out1[1] = Vector.IntersectPlane(plane, normal, insidePoints[0], outsidePoints[0], out t);
                out1.texture[1] = new Vector(t * (outsideTexture[0].X - insideTexture[0].X) + insideTexture[0].X,
                    t * (outsideTexture[0].Y - insideTexture[0].Y) + insideTexture[0].Y,
                    t * (outsideTexture[0].Z - insideTexture[0].Z) + insideTexture[0].Z);
                out1.luminance.Y = t * (outsideLum.X - insideLum.X) + insideLum.X;
                out1[2] = Vector.IntersectPlane(plane, normal, insidePoints[0], outsidePoints[1], out t);
                out1.texture[2] = new Vector(t * (outsideTexture[1].X - insideTexture[0].X) + insideTexture[0].X,
                    t * (outsideTexture[1].Y - insideTexture[0].Y) + insideTexture[0].Y,
                    t * (outsideTexture[1].Z - insideTexture[0].Z) + insideTexture[0].Z);
                out1.luminance.Z = t * (outsideLum.Y - insideLum.X) + insideLum.X;
                out2 = null;
                return 1;
            }
            else if (inside == 2 && outside == 1) // clipped triangle becomes quadrilateral (two triangles)
            {
                out1 = new Triangle();
                out1.luminance = new Vector();
                out1.texture = new Vector[3];
                out2 = new Triangle();
                out2.luminance = new Vector();
                out2.texture = new Vector[3];
                out1.color = triangle.color;
                out2.color = triangle.color;
                // out1 is 2 insidePoints and intersect of plane and one original side
                out1[0] = insidePoints[0];
                out1[1] = insidePoints[1];
                out1.luminance.X = insideLum.X;
                out1.luminance.Y = insideLum.Y;
                out1.texture[0] = insideTexture[0].Copy();
                out1.texture[1] = insideTexture[1].Copy();
                double t;
                out1[2] = Vector.IntersectPlane(plane, normal, insidePoints[0], outsidePoints[0], out t);
                out1.texture[2] = new Vector(t * (outsideTexture[0].X - insideTexture[0].X) + insideTexture[0].X,
                    t * (outsideTexture[0].Y - insideTexture[0].Y) + insideTexture[0].Y,
                    t * (outsideTexture[0].Z - insideTexture[0].Z) + insideTexture[0].Z);
                out1.luminance.Z = t * (outsideLum.X - insideLum.X) + insideLum.X;
                // out2 is 1 inside point and the 2 intersects of original sides and plane
                out2[0] = insidePoints[1];
                out2.texture[0] = insideTexture[1].Copy();
                out2.luminance.X = insideLum.Y;
                out2[1] = out1[2];
                out2.texture[1] = out1.texture[2].Copy();
                out2.luminance.Y = out1.luminance.Z;
                out2[2] = Vector.IntersectPlane(plane, normal, insidePoints[1], outsidePoints[0], out t);
                out2.texture[2] = new Vector(t * (outsideTexture[0].X - insideTexture[1].X) + insideTexture[1].X,
                    t * (outsideTexture[0].Y - insideTexture[1].Y) + insideTexture[1].Y,
                    t * (outsideTexture[0].Z - insideTexture[1].Z) + insideTexture[1].Z);
                out2.luminance.Z = t * (outsideLum.X - insideLum.Y) + insideLum.Y;
                return 2;
            }
            else { out1 = null; out2 = null; return 0; }
        }

        public Matrix WorldTransform(Vector worldPos, Vector rotation)
        {
            Matrix rotX = Matrices.RotateX(rotation.X);
            Matrix rotY = Matrices.RotateY(rotation.Y);
            Matrix rotZ = Matrices.RotateZ(rotation.Z);
            Matrix trans = Matrices.Translate(worldPos);

            Matrix world = Matrix.MultiplyMatrix(rotX, rotZ);
            world = Matrix.MultiplyMatrix(world, rotY);
            world = Matrix.MultiplyMatrix(world, trans);
            return world;
        }

        public Matrix WorldTransformRad(Vector worldPos, Vector rotation)
        {
            Matrix rotX = Matrices.RotateXRad(rotation.X);
            Matrix rotY = Matrices.RotateYRad(rotation.Y);
            Matrix rotZ = Matrices.RotateZRad(rotation.Z);
            Matrix trans = Matrices.Translate(worldPos);

            Matrix world = Matrix.MultiplyMatrix(rotX, rotZ);
            world = Matrix.MultiplyMatrix(world, rotY);
            world = Matrix.MultiplyMatrix(world, trans);
            return world;
        }

        private Vector CalculateNormal(Triangle triangle)
        {
            Vector normal, lineA, lineB;
            lineA = triangle[1].Subtract(triangle[0]);
            lineB = triangle[2].Subtract(triangle[0]);
            normal = Vector.CrossProduct(lineA, lineB);
            return normal.Normalize();
        }

        public void Clear(Color color)
        {
            graphics.Clear(color);
            depthBuffer = new double[rendered.Width, rendered.Height];
        }

        public void SetProjection(double aspectRatio = default, double FOV = 90.0, double Zfar = 1000.0, double Znear = 0.1)
        {
            if (aspectRatio == default) aspectRatio = this.rendered.Height / this.rendered.Width;
            FOV *= Math.PI / 180;
            projection = new Matrix(4, 4)
            {
                [0, 0] = aspectRatio / Math.Tan(FOV / 2),
                [1, 1] = 1.0 / Math.Tan(FOV / 2),
                [2, 2] = Zfar / (Zfar - Znear),
                [2, 3] = 1.0,
                [3, 2] = -Zfar * Znear / (Zfar - Znear)
            };
        }
        public void SetCamera(Vector cameraPos = null, Vector lookDirection = null)
        {
            if (cameraPos == null) cameraPos = new Vector();
            if (lookDirection == null) lookDirection = new Vector(0, 0, 1);
            Vector up = new Vector(0, 1, 0);
            Vector target = cameraPos.Add(lookDirection);
            camera = Matrices.PointAt(cameraPos, target, up);
            camera = Matrices.QuickInverse(camera);
            //camera.Debug();
        }

        /// <summary>
        /// Takes a change of lookdirection in screencoordinates (deltaX, deltaY)
        /// and returns a new lookdirection changed by some magnitudeOfChange.
        /// </summary>
        /// <returns>the new lookdirection</returns>
        public Vector LookWithMouse(Vector lookDirection, double magnitudeOfChange, double deltaX, double deltaY)
        {
            return ApplyMatrix(lookDirection, Matrices.Rotate(magnitudeOfChange, ApplyMatrix(new Vector(-deltaY, deltaX, 0), Matrices.PointAt(new Vector(), lookDirection, new Vector(0, 1, 0))).Normalize()));
        }

        public Vector ApplyMatrix(Vector vector, Matrix matrix, out double w)
        {
            Vector result = new Vector();
            for (int i = 0; i < 3; i++)
                result[i] = matrix[0, i] * vector.X + matrix[1, i] * vector.Y + matrix[2, i] * vector.Z + matrix[3, i];
            w = matrix[0, 3] * vector.X + matrix[1, 3] * vector.Y + matrix[2, 3] * vector.Z + matrix[3, 3];

            if (w != 0)
            {
                result.X /= w;
                result.Y /= w;
                result.Z /= w;
            }
            return result;
        }

        public Vector ApplyMatrix(Vector vector, Matrix matrix)
        {
            Vector result = new Vector();
            for (int i = 0; i < 3; i++)
                result[i] = matrix[0, i] * vector.X + matrix[1, i] * vector.Y + matrix[2, i] * vector.Z + matrix[3, i];
            double w = matrix[0, 3] * vector.X + matrix[1, 3] * vector.Y + matrix[2, 3] * vector.Z + matrix[3, 3];

            if (w != 0)
            {
                result.X /= w;
                result.Y /= w;
                result.Z /= w;
            }
            return result;
        }
    }
}