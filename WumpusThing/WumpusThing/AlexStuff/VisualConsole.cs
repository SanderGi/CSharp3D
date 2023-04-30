using System;
using DrawPanelLibrary;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WumpusThing
{
    class VisualConsole
    {
        private Bitmap overlay;
        private Bitmap content;
        private Graphics cgraphics;

        private string[] buffer;
        private Font font;
        private int charSize;
        private int lineSpacing;
        private Point cursor;

        private DrawingPanel panel;
        private Graphics graphics;

        private Point[] bulgeMap;

        private bool enter;
        private System.Windows.Forms.Form drawingWindow;

        public VisualConsole(DrawingPanel p, int lineSpacing, int charSize)
        {
            this.lineSpacing = lineSpacing;
            this.buffer = new string[p.Height / (charSize + lineSpacing) - 2];
            this.overlay = new Bitmap(p.Width, p.Height);
            this.bulgeMap = VisualEffects.PrecalculateCurvedScreen(p.Width, p.Height, 1.04);
            using (Graphics g = Graphics.FromImage(this.overlay))
            {
                g.DrawImage(new Bitmap("AlexStuff/scanlines.png"), 0, 0);
                g.DrawImage(VisualEffects.Vignette(p.Width, p.Height), 0, 0);
                VisualEffects.PaintBorder(g, 25, 20, p.Width, p.Height);
            }
            this.font = new Font(FontFamily.GenericSansSerif, charSize, FontStyle.Bold, GraphicsUnit.Pixel);
            this.charSize = charSize;
            this.cursor = new Point(0, 0);

            this.panel = p;
            this.graphics = p.GetGraphics();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            this.content = new Bitmap(p.Width, p.Height);
            this.cgraphics = Graphics.FromImage(this.content);
            cgraphics.SmoothingMode = SmoothingMode.AntiAlias;
            cgraphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            this.cgraphics.Clear(Color.DarkGreen);

            this.drawingWindow = this.panel.GetFieldValue<System.Windows.Forms.Form>("drawWindow");
            this.drawingWindow.KeyDown += (sender, eventArgs) =>
            {
                if (eventArgs.KeyCode == System.Windows.Forms.Keys.Enter) this.enter = true;
            };

            // test:
            //System.Threading.Thread.Sleep(1000);
            //this.Shatter(450, 300);
            //System.Threading.Thread.Sleep(30000);
        }

        public void RefreshDisplay()
        {
            Bitmap temporary = (Bitmap)this.content.Clone();
            using (Graphics g = Graphics.FromImage(temporary)) g.DrawImage(this.overlay, 0, 0);
            VisualEffects.MapPixels(temporary, this.bulgeMap);
            this.graphics.Clear(Color.DarkGreen);
            this.graphics.DrawImage(temporary, 0, 0);
            this.panel.RefreshDisplay();
            temporary.Dispose();
        }

        private void CalculateDisplay()
        {
            this.cgraphics.Clear(Color.DarkGreen);
            for (int i = 0; i < this.buffer.Length; i++)
                this.cgraphics.DrawString(this.buffer[i], this.font, Brushes.Cyan, 25, this.charSize * (i + 1) + this.lineSpacing * i);
        }

        public void WriteLine(string message, bool updateScreen = true)
        {
            this.Write(message, updateScreen);
            this.cursor.Y++;
            this.cursor.X = 0;
            if (this.cursor.Y >= this.buffer.Length)
            {
                this.buffer = this.buffer.Skip(1).Append("").ToArray();
                this.cursor.Y--;
                this.cursor.X = 0;
                this.CalculateDisplay();
            }
        }

        public void Write(string message, bool updateScreen = true)
        {
            this.buffer[this.cursor.Y] = (this.buffer[this.cursor.Y] + " ").Insert(this.cursor.X, message);
            this.buffer[this.cursor.Y] = this.buffer[this.cursor.Y].Remove(this.cursor.X + message.Length, Math.Min(message.Length, this.buffer[this.cursor.Y].Length - this.cursor.X - message.Length));
            this.CalculateLine(this.cursor.Y);
            this.cursor.X = this.cursor.X + message.Length;
            if (updateScreen) this.RefreshDisplay();
            GC.Collect();
        }

        private void CalculateLine(int yIndex)
        {
            this.cgraphics.FillRectangle(Brushes.DarkGreen, 25, this.charSize * (yIndex + 1) + this.cursor.Y * this.lineSpacing, this.panel.Width, this.charSize + this.lineSpacing);
            this.cgraphics.DrawString(this.buffer[yIndex], this.font, Brushes.Cyan, 25, this.charSize * (yIndex + 1) + yIndex * this.lineSpacing);
        }

        public string ReadLine(string prompt = "")
        {
            string result = Read(prompt);
            this.WriteLine("");
            return result;
        }

        public string Read(string prompt = "")
        {
            this.panel.Input.FlushKeys();
            if (prompt != "") Write(prompt + "_");
            this.cursor.X--;
            string result = "";
            bool enterNotPressed = true;
            char letter;
            int dontDeletePast = this.cursor.X;
            while (enterNotPressed)
            {
                while (!this.panel.Input.KeyAvailable) {}
                if (this.enter)
                {
                    enterNotPressed = false; this.enter = false;
                } else
                {
                    letter = this.panel.Input.ReadKey();
                    if (letter == '')
                    {
                        if (dontDeletePast < this.cursor.X--)
                        {
                            this.buffer[this.cursor.Y] = this.buffer[this.cursor.Y].Remove(this.cursor.X) + "_";
                            result = result.Remove(result.Length - 1, 1);
                            this.CalculateLine(this.cursor.Y);
                            this.RefreshDisplay();
                            GC.Collect();
                        }
                        else this.cursor.X++;
                    } else
                    {
                        result += letter;
                        this.Write(letter.ToString() + "_");
                        this.cursor.X--;
                    }
                    
                }
            }
            this.buffer[this.cursor.Y] = this.buffer[this.cursor.Y].Remove(this.cursor.X);
            return result;
        }

        public void Shatter(int x, int y)
        {
            Bitmap consoleImage = new Bitmap(this.drawingWindow.Width, this.drawingWindow.Height);
            using (Graphics g = Graphics.FromImage(consoleImage))
            {
                g.CopyFromScreen(new Point(this.drawingWindow.Left, this.drawingWindow.Top), Point.Empty, this.drawingWindow.Size);
            }
            this.graphics.DrawImage(consoleImage, 0, 0);
            this.panel.RefreshDisplay();
        }
    }
}
