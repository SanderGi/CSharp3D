using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console3D
{
    class ScreenBuffer
    {
        char[,] screenBufferArray;
        string screenBuffer;
        int width, height;

        public ScreenBuffer(int width, int height)
        {
            this.screenBufferArray = new char[width, height];
            this.height = height;
            this.width = width;
        }

        public void DrawBuffer(string text, int x, int y)
        {
            char[] arr = text.ToCharArray(0, text.Length);
            int i = 0;
            foreach (char c in arr)
            {
                this.screenBufferArray[x + i, y] = c;
                i++;
            }
        }

        public void DrawScreen(int x = 0, int y = 0)
        {
            this.screenBuffer = "";
            for (int iy = 0; iy < this.height - 1; iy++)
            {
                for (int ix = 0; ix < this.width; ix++)
                {
                    this.screenBuffer += this.screenBufferArray[ix, iy];
                }
                this.screenBuffer += "\n";
            }
            Console.SetCursorPosition(x, y);
            Console.Write(this.screenBuffer);
            this.screenBufferArray = new char[this.width, this.height];
        }

    }
}
