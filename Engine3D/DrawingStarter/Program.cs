using System;
using System.Collections.Generic;
using DrawPanelLibrary; 
using System.Drawing;
using Graphic3D;
using System.Windows.Forms;

namespace Engine3D
{
    class Program
    {
        static void Main(string[] args)
        {
            DrawingPanel panel = new DrawingPanel(900, 600);
            Graphics g = panel.GetGraphics();
            Form drawingWindow = panel.GetFieldValue<Form>("drawWindow");
            Graphics3D graphic3D = new Graphics3D(900, 600);
            //Mesh mesh = new Mesh
            //{
            //    // back
            //    Triangle.FromPoints(0, 0, 0, 0, 1, 0, 1, 1, 0,   0, 1, 0, 0, 1, 0),
            //    Triangle.FromPoints(0, 0, 0, 1, 1, 0, 1, 0, 0,   0, 1, 1, 0, 1, 1),
            //    // left                                     
            //    Triangle.FromPoints(1, 0, 0, 1, 1, 0, 1, 1, 1,   0, 1, 0, 0, 1, 0),
            //    Triangle.FromPoints(1, 0, 0, 1, 1, 1, 1, 0, 1,   0, 1, 1, 0, 1, 1),
            //    // front                                     
            //    Triangle.FromPoints(1, 0, 1, 1, 1, 1, 0, 1, 1,   0, 1, 0, 0, 1, 0),
            //    Triangle.FromPoints(1, 0, 1, 0, 1, 1, 0, 0, 1,   0, 1, 1, 0, 1, 1),
            //    // rigth                                      
            //    Triangle.FromPoints(0, 0, 1, 0, 1, 1, 0, 1, 0,   0, 1, 0, 0, 1, 0),
            //    Triangle.FromPoints(0, 0, 1, 0, 1, 0, 0, 0, 0,   0, 1, 1, 0, 1, 1),
            //    // top                                       
            //    Triangle.FromPoints(0, 1, 0, 0, 1, 1, 1, 1, 1,   0, 1, 0, 0, 1, 0),
            //    Triangle.FromPoints(0, 1, 0, 1, 1, 1, 1, 1, 0,   0, 1, 1, 0, 1, 1),
            //    // bottom                                    
            //    Triangle.FromPoints(1, 0, 1, 0, 0, 1, 0, 0, 0,   0, 1, 0, 0, 1, 0),
            //    Triangle.FromPoints(1, 0, 1, 0, 0, 0, 1, 0, 0,   0, 1, 1, 0, 1, 1)
            //};
            Mesh mesh = new Mesh();
            mesh.LoadFromObjectFile("ObjectFiles/landscape.obj");
            mesh.texture = (Bitmap)Image.FromFile("tetrisLogo.png");
            mesh.RecalculateNormals();

            Vector position = new Vector(); // camera
            Vector lookDirection = new Vector(0, 0, 1); // camera
            double moveIncrement = 0.0008;
            Vector worldPos = new Vector(0, 0, -2);
            Vector meshRotation = new Vector();
            graphic3D.Clear(Color.Black);
            graphic3D.RenderMesh(mesh, worldPos);
            g.Clear(Color.Black);
            graphic3D.DrawRender(g);
            panel.RefreshDisplay();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long counter = 0;
            bool changed = false;
            long dTime;
            Point past = panel.Input.CurrentMousePos; //Mouse stuff
            Point current = past;
            Vector diff;
            drawingWindow.Invoke(new Action(() => {
                //Cursor.Hide();
                drawingWindow.Cursor = Cursors.Cross;
            }));
            while (true)
            {
                dTime = watch.ElapsedMilliseconds;
                counter += dTime;
                System.Threading.Thread.Sleep(1);
                if (dTime < 8) { System.Threading.Thread.Sleep(8); continue; }
                if (counter > 500)
                {
                    drawingWindow.Invoke(new Action(() =>
                    {
                        drawingWindow.Text = "FPS: " + 1000 / dTime;
                    }));
                    counter = 0;
                }
                watch.Restart();

                double move = dTime * moveIncrement;
                Vector delta = lookDirection.Multiply(move);
                if (panel.Input.KeyDown('a')) { position = position.Add(new Vector(delta.Z, 0, -delta.X)); changed = true; }
                if (panel.Input.KeyDown('d')) { position = position.Subtract(new Vector(delta.Z, 0, -delta.X)); changed = true; }
                if (panel.Input.KeyDown('w')) { position = position.Subtract(new Vector(delta.X, 0, delta.Z)); changed = true; }
                if (panel.Input.KeyDown('s')) { position = position.Add(new Vector(delta.X, 0, delta.Z)); changed = true; }
                if (panel.Input.KeyDown(' ')) { position.Y += move; changed = true; }
                if (panel.Input.KeyDown(UI.SpecialKeys.Up)) { moveIncrement += 0.000001 * move; }
                if (panel.Input.KeyDown(UI.SpecialKeys.Down)) { moveIncrement -= 0.000001 * move; }
                if (panel.Input.KeyDown(UI.SpecialKeys.ShiftKey)) { position.Y -= move; changed = true; }
                current = panel.Input.CurrentMousePos;
                diff = new Vector(current.X - past.X, current.Y - past.Y, 0);
                double ldiff = diff.Length();
                if (changed || ldiff != 0)
                {
                    if (ldiff != 0) lookDirection = graphic3D.ApplyMatrix(lookDirection, Matrices.Rotate(ldiff, graphic3D.ApplyMatrix(new Vector(-diff.Y, diff.X, 0), Matrices.PointAt(new Vector(), lookDirection, new Vector(0, 1, 0))).Normalize()));
                    past = new Point(current.X, current.Y);
                    lookDirection = lookDirection.Normalize();
                    graphic3D.SetCamera(position, lookDirection);
                    graphic3D.Clear(Color.Black);
                    graphic3D.RenderMesh(mesh, worldPos, meshRotation);
                    graphic3D.DrawRender(g);
                    panel.RefreshDisplay();
                    changed = false;
                }
            }
        }
    }
}
