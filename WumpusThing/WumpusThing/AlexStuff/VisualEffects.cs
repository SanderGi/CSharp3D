using System;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WumpusThing
{
    static class VisualEffects
    {
        public static void PaintVignette(Graphics g, Rectangle bounds)
        {
            Rectangle ellipsebounds = bounds;
            ellipsebounds.Offset(-ellipsebounds.X - 20, -ellipsebounds.Y);
            int x = ellipsebounds.Width - (int)Math.Round(.70712 * ellipsebounds.Width);
            int y = ellipsebounds.Height - (int)Math.Round(.70712 * ellipsebounds.Height);
            ellipsebounds.Inflate(x, y);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(ellipsebounds);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.WrapMode = WrapMode.Tile;
                    brush.CenterColor = Color.FromArgb(0, 0, 0, 0);
                    brush.SurroundColors = new Color[] { Color.FromArgb(255, 0, 0, 0) };
                    Blend blend = new Blend();
                    blend.Positions = new float[] { 0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0F };
                    blend.Factors = new float[] { 0.0f, 0.5f, 1f, 1f, 1.0f, 1.0f };
                    brush.Blend = blend;
                    Region oldClip = g.Clip;
                    g.Clip = new Region(bounds);
                    g.FillRectangle(brush, ellipsebounds);
                    g.Clip = oldClip;
                }
            }
        }

        public static Bitmap Vignette(int width, int height)
        {
            Bitmap final = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(final))
            {
                PaintVignette(g, new Rectangle(0, 0, width, height));
                return final;
            }
        }

        public static void PaintBorder(Graphics g, int xFrameSize, int yFrameSize, int width, int height)
        {
            g.FillRectangle(Brushes.Black, -1, -1, width, yFrameSize);
            g.FillRectangle(Brushes.Black, 0, height - yFrameSize, width, yFrameSize);
            g.FillRectangle(Brushes.Black, -1, 0, xFrameSize, height);
            g.FillRectangle(Brushes.Black, width - xFrameSize, 0, xFrameSize, height);
        }

        public static unsafe void MapPixels(Bitmap image, Point[] map)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            Bitmap empty = new Bitmap(image.Width, image.Height);
            BitmapData newImageData = empty.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            int bytesPerPixel = 3;

            byte* scan0 = (byte*)imageData.Scan0.ToPointer();
            int stride = imageData.Stride;
            byte* newScan0 = (byte*)newImageData.Scan0.ToPointer();
            int newStride = newImageData.Stride;
            Task[] tasks = new Task[4];
            for (int i = 0; i < tasks.Length; i++)
            {
                int ii = i;
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    int minY = ii * imageData.Height / tasks.Length;
                    int maxY = minY + imageData.Height / tasks.Length;
                    for (int y = minY; y < maxY; y++)
                    {
                        byte* row = newScan0 + (y * newStride);
                        for (int x = 0; x < imageData.Width; x++)
                        {
                            byte* originRow = scan0 + (map[x + imageData.Width * y].Y * stride);
                            int originIndexB = map[x + imageData.Width * y].X * bytesPerPixel;
                            int originIndexG = originIndexB + 1;
                            int originIndexR = originIndexB + 2;
                            byte pixelR = originRow[originIndexR];
                            byte pixelG = originRow[originIndexG];
                            byte pixelB = originRow[originIndexB];

                            int bIndex = x * bytesPerPixel;
                            int gIndex = bIndex + 1;
                            int rIndex = bIndex + 2;
                            row[rIndex] = pixelR;
                            row[bIndex] = pixelB;
                            row[gIndex] = pixelG;
                        }
                    }
                });
            }
            Task.WaitAll(tasks);
            image.UnlockBits(newImageData);
            empty.UnlockBits(imageData); // Fixes memory leak
        }

        public static Point[] PrecalculateCurvedScreen(int width, int height, double bulgeFactor)
        {
            Point[] map = new Point[width * height];
            for (int i = 0; i < width; i++)
            {
                double x = (double)i / width - 0.5;
                for (int j = 0; j < height; j++)
                {
                    double y = (double)j / height - 0.5;
                    double r = Math.Sqrt(x * x + y * y);
                    double a = Math.Atan2(y, x);
                    double rn = Math.Pow(r, bulgeFactor);
                    int mapX = (int)Math.Round((rn * Math.Cos(a) + 0.5) * width);
                    int mapY = (int)Math.Round((rn * Math.Sin(a) + 0.5) * height);
                    if (mapX >= 0 && mapX < width && mapY >= 0 && mapY < height)
                    {
                        map[i + width * j].X = mapX;
                        map[i + width * j].Y = mapY;
                    } else
                    {
                        map[i + width * j].X = 0;
                        map[i + width * j].Y = 0;
                    }
                }
            }
            return map;
        }

        public static void DrawGrid(Graphics graphics, int width, int height, int spacing)
        {
            for (int i = spacing; i < width; i += spacing)
            {
                graphics.DrawLine(new Pen(Color.FromArgb(i / width * 255, 0, 200)), i, 0, i, height);
            }
            for (int i = spacing; i < height; i += spacing)
            {
                graphics.DrawLine(new Pen(Color.FromArgb(0, i / height * 255, 200)), 0, i, width, i);
            }
        }
    }
}
