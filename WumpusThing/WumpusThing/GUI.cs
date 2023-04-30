using System;
using DrawPanelLibrary;
using System.Windows.Forms;
using Engine3D;
using System.Drawing;
using System.Drawing.Imaging;

namespace WumpusThing
{
    class GUI
    {
        private GameControl game; // used for communicating with the rest of the game classes

        private DrawingPanel panel;
        private Graphics graphics; // used for 2d stuff drawing on panel
        private Form drawingWindow; // the form inside the panel gives us more control over the window, the mouse, etc.
        private System.Media.SoundPlayer player; // who doesn't like some background music...

        private VisualConsole console; // console like thing used for the start menu 
        private Graphics3D graphic3D; // used for rendering 3D models
        private Mesh ship; // 3d model of a spaceship
        private Mesh asteriod; // 3d model of a rock
        private Vector[] asteroidPositions; // where the asteroid is to be rendered
        private Mesh skybox; // 3d model of a cube

        const double TAU = Math.PI * 2; // useful constant
        Random rand; // used for generating random numbers

        /// <summary>
        /// Main entry point for program
        /// </summary>
        static void Main()
        {
            GUI ui = new GUI();
        }

        public GUI()
        {
            InitializeMusic();
            InitializeGameFunctionality();
            Initialize3DModels();

            StartMenu();
        }

        private void Initialize3DModels()
        {
            this.ship = new Mesh();
            this.ship.LoadFromObjectFile("AlexStuff/Assets/StarSparrow01.obj", new Bitmap("AlexStuff/Assets/StarSparrow_Green.png"));

            this.asteriod = new Mesh();
            this.asteriod.LoadFromObjectFile("AlexStuff/Assets/Rock_1.obj", new Bitmap("AlexStuff/Assets/Rock_1_Base_Color.jpg"));
            this.asteroidPositions = new Vector[6];

            this.rand = new Random();
            for (int i = 0; i < 6; i++)
                this.asteroidPositions[i] = new Vector(this.rand.Next(-400, 400), this.rand.Next(-50, 50), this.rand.Next(-400, 400));

            this.skybox = new Mesh
            {
                // bottom
                Triangle.FromPoints(-1000, -1000, -1000, -1000, -1000, 1000 , 1000 , -1000, 1000 ,     0.6671009772, 1, 0.6671009772, 0.5004887586, 1, 0.5004887586),
                Triangle.FromPoints(-1000, -1000, -1000, 1000 , -1000, 1000 , 1000 , -1000, -1000,     0.6671009772, 1, 1, 0.5004887586, 1, 1),
                // front
                Triangle.FromPoints(1000 , -1000, -1000, 1000 , 1000 , -1000, -1000, 1000 , -1000,     0, 1, 0, 0.5004887586, 0.3328990228, 0.5004887586),
                Triangle.FromPoints(1000 , -1000, -1000, -1000, 1000 , -1000, -1000, -1000, -1000,     0, 1, 0.3328990228, 0.5004887586, 0.3328990228, 1),
                // back
                Triangle.FromPoints(-1000, -1000, 1000 , -1000, 1000 , 1000 , 1000 , 1000 , 1000 ,     0, 0.4995112414, 0, 0, 0.3328990228, 0),
                Triangle.FromPoints(-1000, -1000, 1000 , 1000 , 1000 , 1000 , 1000 , -1000, 1000 ,     0, 0.4995112414, 0.3328990228, 0, 0.3328990228, 0.4995112414),
                // left                                     
                Triangle.FromPoints(-1000, -1000, -1000, -1000, 1000 , -1000, -1000, 1000 , 1000 ,     0.6671009772, 0.4995112414, 0.6671009772, 0, 1, 0),
                Triangle.FromPoints(-1000, -1000, -1000, -1000, 1000 , 1000 , -1000, -1000, 1000 ,     0.6671009772, 0.4995112414, 1, 0, 1, 0.4995112414),
                // rigth                                      
                Triangle.FromPoints(1000 , -1000, 1000 , 1000 , 1000 , 1000 , 1000 , 1000 , -1000,     0.3335504886, 0.4995112414, 0.3335504886, 0, 0.6664495114, 0),
                Triangle.FromPoints(1000 , -1000, 1000 , 1000 , 1000 , -1000, 1000 , -1000, -1000,     0.3335504886, 0.4995112414, 0.6664495114, 0, 0.6664495114, 0.4995112414),
                // top                                       
                Triangle.FromPoints(-1000, 1000 , 1000 , -1000, 1000 , -1000, 1000 , 1000 , -1000,     0.3335504886, 1, 0.3335504886, 0.5004887586, 0.6664495114, 0.5004887586),
                Triangle.FromPoints(-1000, 1000 , 1000 , 1000 , 1000 , -1000, 1000 , 1000 , 1000 ,     0.3335504886, 1, 0.6664495114, 0.5004887586, 0.6664495114, 1),
            };
            this.skybox.texture = new Bitmap("AlexStuff/Assets/skybox.png");
            this.skybox.maxLight = true;
        }

        private void InitializeGameFunctionality()
        {
            this.panel = new DrawingPanel(900, 600);
            this.game = new GameControl();
            this.SetUpForm();
            this.graphics = this.panel.GetGraphics();
            this.panel.Menus.TakeOverFileExitDuties(); // Now our code (that we control!) decides how to handle File/Exit
            this.console = new VisualConsole(this.panel, 5, 20);
            this.graphic3D = new Graphics3D(900, 600);
        }

        private void InitializeMusic()
        {
            this.player = new System.Media.SoundPlayer();
            this.player.SoundLocation = "AlexStuff/Assets/bensound-newdawn.wav";
        }

        /// <summary>
        /// Purely for code writing/optimizing purposes. Let's me quickly access how fast the 3D rendering is.
        /// </summary>
        private void Analyze3DPerformance(int iterations)
        {
            Vector position = new Vector(); // ship position
            Vector lookDirection = new Vector(0, 0, -1); // camera lookdirection (note axis directions are flipped)
            this.graphic3D.SetCamera(position.Add(new Vector(0, 3, -18)), lookDirection);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long start = watch.ElapsedMilliseconds;
            for (int i = 0; i < iterations; i++)
            {
                foreach (Vector place in this.asteroidPositions)
                {
                    //if (Vector.SignedShortestDist(place, new Vector(lookDirection.X, lookDirection.Y, -lookDirection.Z), new Vector(0.0, 0.0, -0.1)) >= 0)
                    if (this.graphic3D.IsVisible(place, 25))
                        this.graphic3D.RenderMeshRad(this.asteriod, place);
                }
                for (int n = 0; n < 6; n++)
                    this.asteroidPositions[n] = new Vector(this.rand.Next(-400, 400), this.rand.Next(-50, 50), this.rand.Next(-400, 400));
            }
            Console.WriteLine("Asteroids ms: " + (watch.ElapsedMilliseconds - start) / iterations);
            start = watch.ElapsedMilliseconds;
            for (int i = 0; i < iterations; i++)
            {
                this.graphic3D.RenderSkyboxRad(this.skybox);
            }
            Console.WriteLine("Skybox ms: " + (watch.ElapsedMilliseconds - start) / iterations);
            start = watch.ElapsedMilliseconds;
            for (int i = 0; i < iterations; i++)
            {
                this.graphic3D.RenderMeshRad(this.ship, position);
            }
            Console.WriteLine("Ship ms: " + (watch.ElapsedMilliseconds - start) / iterations);
            Console.WriteLine("Total triangles: " + (this.ship.Count + this.skybox.Count + this.asteriod.Count * this.asteroidPositions.Length));
            this.graphic3D.DrawRender(this.graphics);
            this.panel.RefreshDisplay();
            while (!this.panel.Input.KeyAvailable) { }
            //Console.WriteLine("Asteroid triangles: " + this.asteriod.Count * this.asteroidPositions.Length);
            //Console.WriteLine("Skybox triangles: " + this.skybox.Count);
            //Console.WriteLine("Ship triangles: " + this.ship.Count);
        }

        /// <summary>
        /// Gets access to the form, sets its position and fixes it's size.
        /// </summary>
        private void SetUpForm()
        {
            this.drawingWindow = this.panel.GetFieldValue<Form>("drawWindow"); // get eccess to the form inside the 'panel' (this field is normally private so we access it using a method found in "ReflectionExtension.cs")
            this.drawingWindow.Text = "EPIC WUMPUS GAME"; // set window title
            this.drawingWindow.FormBorderStyle = FormBorderStyle.FixedDialog; // Don't allow user to resize window
            this.drawingWindow.MaximizeBox = false;
            this.drawingWindow.MinimizeBox = false;
            this.drawingWindow.StartPosition = FormStartPosition.CenterScreen; // Display in center of screen
        }

        /// <summary>
        /// Plays through a game and updates the leaderboard accordingly.
        /// </summary>
        private void PlayGame()
        {
            this.player.PlayLooping(); // start music

            // INITIALIZE A BUNCH OF VARIABLES
            Bitmap speedometer = new Bitmap("AlexStuff/Assets/Speedometer.png"); // load some images
            Bitmap blaster = new Bitmap("AlexStuff/Assets/laserBlaster.png");
            Bitmap breach = new Bitmap("AlexStuff/Assets/breach.png");
            Vector[] breachPositions = new Vector[] { new Vector(this.rand.Next(-400, 400), this.rand.Next(-50, 50), this.rand.Next(-400, 400)),
                new Vector(this.rand.Next(-400, 400), this.rand.Next(-50, 50), this.rand.Next(-400, 400)),
                new Vector(this.rand.Next(-400, 400), this.rand.Next(-50, 50), this.rand.Next(-400, 400))};
            int[] breachDestinations = new int[] { 1, 2, 3 }; // this is the places that can be visited from the current dimension

            int currentDimension = 0;
            int blastsLeft = 5;
            int nearestDimension = 1;
            string[] hazardsNearby = new string[] { "Spikes in the gravitational field", "Reports of nearby imperial blockade", "The Wumpus tracker is blinking" };

            Vector position = new Vector(); // ship position
            Vector lookDirection = new Vector(0, 0, -1); // camera lookdirection (note: the positive and negative directions here are not the same as for everything else)
            this.graphic3D.SetCamera(position, lookDirection);
            double speed = 0.0; // initial ship speed
            const double acceleration = 0.000008; // ship acceleration is small because time is measured in milliseconds (ms)

            Vector offset = new Vector(0, 3, -9); // camera position relative to ship
            Vector shipDirection = new Vector(lookDirection.X, lookDirection.Y, -lookDirection.Z); // the direction the ship is flying
            Vector shipRotation = new Vector(); // rotation.X - pitch, rotation.Y - yaw, rotation.Z - roll
            Vector targetRotation = new Vector();

            // drawing images with transparency requires using ImageAttributes with transpanrency in the ColorMatrix
            ColorMatrix matrix = new ColorMatrix();
            matrix.Matrix33 = 0.3f;
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            Point past = this.panel.Input.CurrentMousePos; // Mouse stuff
            bool mouseDown = false;
            Point current = past;
            Vector diff = null;
            Form window = this.drawingWindow;
            window.Invoke(new Action(() =>
            {
                window.Cursor = Cursors.NoMove2D; // cool-looking mouse cursor to indicate user can hold down and move to look around
            }));

            // prepare some fonts for UI so they don't have to be declared everytime they need to be used
            Font font = new Font(FontFamily.GenericSansSerif, 20f);
            Font font2 = new Font(FontFamily.GenericSansSerif, 10f);

            // GAME LOOP
            while (currentDimension > -1) // keep exploring new dimensions until game is over (currentDimension is negative)
            {
                currentDimension = this.ExploreDimension(this.panel, this.graphics, this.graphic3D, this.ship, this.asteriod, this.asteroidPositions,
                this.skybox, window, speedometer, blaster, breach, breachPositions, breachDestinations,
                currentDimension, ref blastsLeft, nearestDimension, hazardsNearby,
                position, lookDirection, ref speed, acceleration,
                ref offset, shipDirection, shipRotation, targetRotation, attributes,
                ref past, ref mouseDown, ref current, diff, font, font2);
            }

            // game is finished:
            font.Dispose(); // now we don't need those fonts anymore
            font2.Dispose();
            this.player.Stop(); // stop music
        }

        /// <summary>
        /// Let's the player explore a given dimension (fly around and shoot blasters) until they jump to the next one.
        /// </summary>
        /// <returns>The next dimension (negative if the game is over)</returns>
        private int ExploreDimension(DrawingPanel panel, Graphics graphics, Graphics3D graphics3D, Mesh ship, Mesh asteriod, Vector[] asteriodPositions, 
            Mesh skybox, Form window, Bitmap speedometer, Bitmap blaster, Bitmap breach, Vector[] breachPositions, int[] breachDestinations, 
            int currentDimension, ref int blastsLeft, int nearestDimension, string[] hazardsNearby,
            Vector position, Vector lookDirection, ref double speed, double acceleration, 
            ref Vector offset, Vector shipDirection, Vector shipRotation, Vector targetRotation, ImageAttributes attributes, 
            ref Point past, ref bool mouseDown, ref Point current, Vector diff, Font font, Font font2)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew(); // FPS stuff
            long counter = 0;
            bool changed = true; // If the screen has changed and it's necessary to render it.
            long dTime = 0; // The time difference between frames in milliseconds (ms)
            while (true)
            {
                dTime = watch.ElapsedMilliseconds;
                counter += dTime;
                System.Threading.Thread.Sleep(1);
                if (dTime < 8) { System.Threading.Thread.Sleep(8); continue; } // If the previous frame finished quicker than 8 ms, let's rest for a while.
                watch.Restart();
                if (counter > 500) // These things don't need to change that often, let's only do them after a 'counter' of 500 ms has passed.
                {
                    window.Invoke(new Action(() =>
                    {
                        // Sets the title to indicate FPS on the window's thread (hence Invoke).
                        window.Text = "EPIC WUMPUS GAME--FPS: " + 1000 / dTime;
                    }));
                    nearestDimension = CalculateNearestBreach(breachPositions, breachDestinations, position);
                    counter = 0; // now that's done, let's restart counting to 500 before doing this again.
                }

                double move = speed * dTime; // changes in ship 'position' has to be proportional to the time passed and the 'speed' value
                HandleMouseInput(panel, graphics3D, window, ref lookDirection, targetRotation, ref past, ref mouseDown, ref current, ref diff, ref changed);
                HandleKeyboardInput(panel, ref speed, acceleration, offset, targetRotation, ref changed, dTime);

                // allows me to jump dimension with keyboard. Only for debugging.
                if (panel.Input.KeyDown(UI.SpecialKeys.Escape)) return -1;
                if (panel.Input.KeyDown('j')) return nearestDimension;

                UpdateShipTransform(graphics3D, ref position, ref shipDirection, shipRotation, targetRotation, ref changed, move);

                if (changed)
                {
                    // Set the camera to look in lookDirection as some 'offset' to the ship 
                    // and make sure that the 'offset' accounts for shipRotation so the user can see the ship from a good angle
                    graphics3D.SetCamera(position.Add(graphics3D.ApplyMatrix(offset,
                        Matrix.MultiplyMatrix(Matrices.RotateXRad(shipRotation.X - 0.2), Matrices.RotateYRad(shipRotation.Y)))),
                        lookDirection);

                    Render3D(graphics, graphics3D, ship, asteriod, asteriodPositions, skybox, breach, breachPositions, position, shipRotation);
                    RenderUI(graphics, speedometer, blaster, currentDimension, blastsLeft, nearestDimension, hazardsNearby, speed, attributes, font, font2);
                    panel.RefreshDisplay();
                    changed = false; // after rendering, there's now nothing new to render
                }
            }
        }

        /// <summary>
        /// Since the ship doesn't just move instantly as the user presses a key, but instead moves depending on speed and acceleration, 
        /// it needs to update its transform (position and rotation/direction) each frame by interpolating between its current and target transforms.
        /// </summary>
        private void UpdateShipTransform(Graphics3D graphics3D, ref Vector position, ref Vector shipDirection, Vector shipRotation, Vector targetRotation, ref bool changed, double move)
        {
            if (Math.Abs(move) > 0.02) // only do something if the ship is 'move'-ing 
            {
                shipRotation.Y = LerpAngleRadians(shipRotation.Y, targetRotation.Y, move * 0.03); // yaw
                shipRotation.X = LerpAngleRadians(shipRotation.X, Math.Abs(targetRotation.X) < 0.5 ? targetRotation.X : Math.Sign(targetRotation.X) * 0.5, move * 0.08); // pitch
                shipRotation.Z = Lerp(shipRotation.Z, targetRotation.Z, move * 0.1); // roll (aproximately)

                // standard conversion of pitch and yaw to a direction vector.
                shipDirection = new Vector(Math.Sin(shipRotation.Y) * Math.Cos(shipRotation.X),
                    Math.Sin(shipRotation.X),
                    -Math.Cos(shipRotation.Y) * Math.Cos(shipRotation.X));

                // change ship 'position' in the shipDirection 
                // but make the ship tend in the direction of the roll (tilt in any wing direction)
                position = position.Subtract(graphics3D.ApplyMatrix(shipDirection, Matrices.RotateYRad(2 * shipRotation.Z)).Normalize().Multiply(move * 5));
                changed = true; // if the ship has moved, the screen needs to be redrawn.
            }
        }

        /// <summary>
        /// Handles keyboard input when the user is flying around using if statements to check what keys are currently down.
        /// Appropriatly sets 'changed' to true if keyboard input results in a change to the screen.
        /// </summary>
        private static void HandleKeyboardInput(DrawingPanel panel, ref double speed, double acceleration, Vector offset, Vector targetRotation, ref bool changed, long dTime)
        {
            if (panel.Input.KeyDown('w')) speed = Math.Min(speed + acceleration * dTime, 0.01);
            else if (panel.Input.KeyDown('s')) speed = Math.Max(speed - acceleration * dTime, 0);
            targetRotation.Z = 0;
            if (panel.Input.KeyDown('a')) targetRotation.Z = 0.5;
            else if (panel.Input.KeyDown('d')) targetRotation.Z = -0.5;
            if (panel.Input.KeyDown(UI.SpecialKeys.Up)) { offset.Z += 0.2; changed = true; }
            else if (panel.Input.KeyDown(UI.SpecialKeys.Down)) { offset.Z -= 0.2; changed = true; }
            if (panel.Input.KeyDown(UI.SpecialKeys.Left)) { offset.X -= 0.2; changed = true; }
            else if (panel.Input.KeyDown(UI.SpecialKeys.Right)) { offset.X += 0.2; changed = true; }
            if (panel.Input.KeyDown(UI.SpecialKeys.ShiftKey)) { offset.Y -= 0.2; changed = true; }
            else if (panel.Input.KeyDown(' ')) { offset.Y += 0.2; changed = true; }
        }

        /// <summary>
        /// Loops through the breachpositions and calculates which one is closer to the ship 'position'.
        /// </summary>
        /// <returns>the integer associated with the closest dimension</returns>
        private static int CalculateNearestBreach(Vector[] breachPositions, int[] breachDestinations, Vector position)
        {
            int nearestDimension;
            double closestSqDistance = 99999999999;
            int closestBreach = 0;
            for (int i = 0; i < breachPositions.Length; i++)
            {
                Vector relativePosition = breachPositions[i].Subtract(position); // distance has to be relative to ship, not origin
                double distanceSquared = relativePosition.X * relativePosition.X + relativePosition.Y * relativePosition.Y + relativePosition.Z * relativePosition.Z; // pythagoras but for 3D
                if (closestSqDistance > distanceSquared)
                {
                    // if current distance is closer than the previously closest, make that the new closest
                    closestSqDistance = distanceSquared;
                    closestBreach = i;
                }
            }
            nearestDimension = breachDestinations[closestBreach];
            return nearestDimension;
        }

        /// <summary>
        /// Renders the 3D objects such as asteroids, spaceship and skybox.
        /// </summary>
        private static void Render3D(Graphics graphics, Graphics3D graphics3D, Mesh ship, Mesh asteriod, Vector[] asteriodPositions, Mesh skybox, Bitmap breach, Vector[] breachPositions, Vector position, Vector shipRotation)
        {
            graphics3D.Clear(Color.Black);
            // render an asteroid at all visible asteriodPositions
            foreach (Vector place in asteriodPositions)
            {
                // instead of letting the graphics3D check for every triangle if it's visible, 
                // when we don't know if an object will be visible, 
                // it's a good idea to check once for the entire object in advance:
                if (graphics3D.IsVisible(place, 25)) 
                    graphics3D.RenderMeshRad(asteriod, place);
            }

            // render the skybox using RenderSkyboxRad() which is optimized for big triangles that cover large parts of the screen.
            graphics3D.RenderSkyboxRad(skybox, position);

            // render the breaches (these a technically rendered using c# default graphics object because they're round, 
            // however their size and position is determined by the graphics3D)
            foreach (Vector breachPos in breachPositions)
            {
                Vector topLeft = graphics3D.ProjectPosition(breachPos);
                Vector bottomRight = graphics3D.ProjectPosition(breachPos.Add(new Vector(20, 20, 0)));
                if (topLeft != null && bottomRight != null)
                    graphics3D.graphics.DrawImage(breach, (float)topLeft.X, (float)topLeft.Y, (float)Math.Abs(bottomRight.X - topLeft.X), (float)Math.Abs(bottomRight.Y - topLeft.Y));
            }

            // renders the space ship
            graphics3D.RenderMeshRad(ship, position, shipRotation);

            graphics3D.DrawRender(graphics); // Draws the graphics3D bitmap to the panel
        }

        /// <summary>
        /// Draws the 2D UI elements such as speedometer and text to panel.
        /// </summary>
        private static void RenderUI(Graphics graphics, Bitmap speedometer, Bitmap blaster, int currentDimension, int blastsLeft, int nearestDimension, string[] hazardsNearby, double speed, ImageAttributes attributes, Font font, Font font2)
        {
            double normSpeed = speed * 100; // adjust the speed displayed to user since it's a small number
            // The speedometer is split into 2 images at some height determined by the speed
            graphics.DrawImage(speedometer, new Rectangle(0, 400, 122, (int)(200 * (1 - normSpeed))), 0, 0, 371, (int)(607 * (1 - normSpeed)), GraphicsUnit.Pixel, attributes);
            graphics.DrawImage(speedometer, new Rectangle(0, 400 + (int)(200 * (1 - normSpeed)), 122, (int)(200 * normSpeed)), 0, (int)(607 * (1 - normSpeed)), 371, (int)(607 * normSpeed), GraphicsUnit.Pixel);
            // Displays the current and nearest dimension (as number) in the upper-right corner of screen:
            graphics.DrawString($"Dimension: {currentDimension:000}", font, Brushes.White, 700, 0);
            graphics.DrawString($"Nearest: {nearestDimension:000}", font, Brushes.White, 735, 30);
            // Draws the blaster icon as well as the number of blasts not used:
            graphics.DrawImage(blaster, 795, 495, 100, 99);
            graphics.DrawString(blastsLeft.ToString(), font, Brushes.Cyan, 870, 563);
            // Displays the list of hazardsNearby in the upper-left corner of screen:
            for (int i = 0; i < hazardsNearby.Length; i++)
            {
                graphics.DrawString(hazardsNearby[i], font2, Brushes.White, 0, 15 * i);
            }
        }

        /// <summary>
        /// If mousedown, make sure the lookdirection is updated to match where user looks with mouse. 
        /// Update the target direction of the spaceship accordingly so it knows where the user is trying to go.
        /// </summary>
        private static void HandleMouseInput(DrawingPanel panel, Graphics3D graphics3D, Form window, ref Vector lookDirection, Vector targetRotation, ref Point past, ref bool mouseDown, ref Point current, ref Vector diff, ref bool changed)
        {
            if (panel.Input.MouseButtonDown)
            {
                current = panel.Input.CurrentMousePos;
                if (mouseDown) // if mouse is currently moving (was down before and is down now)
                {
                    diff = new Vector(current.X - past.X, current.Y - past.Y, 0); // the change in mouseposition
                    double ldiff = diff.Length();
                    if (ldiff != 0) // if magnitude of change is not zero
                    {
                        lookDirection = graphics3D.LookWithMouse(lookDirection, ldiff, diff.X, diff.Y);
                        // Calculate pitch and jaw of ship based on the lookDirection:
                        targetRotation.Y = Math.Atan2(lookDirection.X, -lookDirection.Z);
                        targetRotation.X = Math.Atan2(lookDirection.Y, Math.Sqrt(lookDirection.Z * lookDirection.Z + lookDirection.X * lookDirection.X));
                        // Update mouseposition and let render function know screen changed:
                        past = new Point(current.X, current.Y);
                        changed = true;
                    }
                }
                else // if the mouse is down now but wasn't before, hide the cursor and take note of the mouse position.
                {
                    window.BeginInvoke(new Action(() => Cursor.Hide()));
                    mouseDown = true;
                    past = current;
                }
            }
            else if (mouseDown) // if the mouse was down previously but not now, unhide (show) the mouse cursor.
            {
                window.BeginInvoke(new Action(() => Cursor.Show()));
                mouseDown = false;
            }
        }

        /// <summary>
        /// Math function for calculating a number between 'start' and 'end' depending on 'by'. 
        /// </summary>
        /// <param name="by">In range [0, 1]: closer to 0 means closer to 'start'</param>
        private double Lerp(double start, double end, double by)
        {
            if (by > 1) by = 1;
            if (by < 0) by = 0;
            return start * (1 - by) + end * by;
        }

        /// <summary>
        /// Math function for calculating an angle between two angles (start and end) depending on 'by'. 
        /// Considers 6 closer to 1 than 2 due to the modular nature of angles.
        /// </summary>
        /// <param name="by">
        /// Between 0 and 1 (inclusive). 
        /// Closer to 1 indicates angles closer to 'end' and closer to 0 indicates closer to 'start'.
        /// </param>
        private double LerpAngleRadians(double start, double end, double by)
        {
            double difference = (end - start) % TAU;
            double shortAngle = (2 * difference) % TAU - difference;
            return start + shortAngle * by;
        }

        /// <summary>
        /// Runs the start menu that looks like a console and calls PlayGame() when the user types "start".
        /// </summary>
        private void StartMenu()
        {
            VisualConsole c = this.console;
            this.drawingWindow.Invoke(new Action(() => { // Things related to the window has to be done on the thread that created the window (hence Invoke)
                this.drawingWindow.Cursor = Cursors.IBeam; // Sets the look of the mouse cursor to look like an I (just like real console)
            }));

            c.WriteLine("Hunt The Wumpus [Version 0.0.0.1]", false);
            c.WriteLine("(c) 2020 WumpusThing Corporation. No rights reserved.", false);
            c.WriteLine("", false);

            string startMenuCommand; // stores the user inputted string at the beggining of each loop
            bool running = true;
            while (running) // keep listening for input and when there's input act accordingly
            {
                startMenuCommand = c.ReadLine("C:\\Adventurers\\WumpusHunter>");
                running = HandleUserCommand(c, startMenuCommand);
                c.WriteLine("");
            }
            // McT: random comment, pretending to be an important change.
        }

        /// <summary>
        /// A switch to identify the user inputted string and write a corresponding responce to the console.
        /// Calls PlayGame() if the user typed "start".
        /// </summary>
        /// <returns>bool indicating if the user hasn't exited yet</returns>
        private bool HandleUserCommand(VisualConsole c, string startMenuCommand)
        {
            switch (startMenuCommand.ToLower())
            {
                case "help":
                    c.WriteLine("START\t\tStarts the Wumpus hunting game.", false);
                    c.WriteLine("EXIT\t\tExits the application.", false);
                    c.WriteLine("SCORES\tDisplays the top 10 Wumpus hunters.", false);
                    c.WriteLine("CONTROLS\tOverview of in game controls.", false);
                    c.WriteLine("ABOUT\t\tShows credits.", false);
                    c.WriteLine("HELP\t\tHmm... wonder what this does.", false);
                    break;
                case "start":
                    c.WriteLine("Hello there, green commander.", false);
                    c.WriteLine("", false);
                    c.WriteLine("Your mission is to hunt down the infamous Wumpus.", false);
                    c.WriteLine("To do that, you must navigate the multiverse in your spaceship by flying into breaches.", false);
                    c.WriteLine("Breaches are blue portals that allow you to enter other dimensions.", false);
                    c.WriteLine("Careful, though, breaches are from a fractal dimension and behave weirdly.", false);
                    c.WriteLine("This makes them hard to spot.", false);
                    c.WriteLine("", false);
                    c.WriteLine("Note: when you are near the breach with the Wumpus, DO NOT ENTER!", false);
                    c.WriteLine("It will kill you. You must shoot one of you 5 laser blasts (press q).", false);
                    c.WriteLine("", false);
                    c.ReadLine("Press enter when you are ready.");
                    c.WriteLine("Launching spaceship...");
                    this.PlayGame();
                    break;
                case "controls":
                    c.WriteLine("mouse\t\tHold down and move in the direction you wanna look.", false);
                    c.WriteLine("\t\tThe spaceship will slowly follow depending on its speed.", false);
                    c.WriteLine("w\t\tIncrease speed.", false);
                    c.WriteLine("s\t\tDecrease speed.", false);
                    c.WriteLine("q\t\tShoot missile towards nearest portal.", false);
                    c.WriteLine("esc\t\tLeave game (warning: doesn't save progress).", false);
                    c.WriteLine("a\t\tTilt left.", false);
                    c.WriteLine("d\t\tTilt right.", false);
                    c.WriteLine("arrows\t\tMove camera relative to spaceship.", false);
                    c.WriteLine("spacebar\tMove camera up.", false);
                    c.WriteLine("shift\t\tMove camera down.", false);
                    break;
                case "exit":
                    c.WriteLine("Exiting...");
                    return false;
                case "win":
                    c.WriteLine("You do not have permission to execute 'win' command.");
                    break;
                case "sudo win":
                    c.WriteLine("Lol. Ok, you win :)");
                    break;
                case "lose":
                    c.WriteLine("Sorry. This game is not as realistic as life: you're not a loser.");
                    break;
                case "pi":
                    c.WriteLine("3.141592653589793238... (or simply 4)");
                    break;
                case "tau":
                    c.WriteLine("Ah, yes. A man of culture.");
                    break;
                case "scores":
                    c.WriteLine("1. Alex M\t\t999,999,999", false);
                    c.WriteLine("2. NotABot\t\t100", false);
                    c.WriteLine("3. GhostKing\t\t50", false);
                    c.WriteLine("4. Lord_Bob\t\t-1000");
                    break;
                case "about":
                    c.WriteLine("Main Developers: ", false);
                    c.WriteLine("Trivia & game mechanics -- Charlotte", false);
                    c.WriteLine("Game mechanics -- Winston", false);
                    c.WriteLine("Graphics engine & Game mechanics -- Alexander", false);
                    c.WriteLine("", false);
                    c.WriteLine("Assets: ", false);
                    c.WriteLine("Royalty Free Music from Bensound", false);
                    c.WriteLine("Spaceship 3D model by EBAL STUDIOS", false);
                    c.WriteLine("Skybox by StumpyStrust on OpenGameArt.org", false);
                    c.WriteLine("Asteroid 3D model by GizemDilaraSaatci on cgtrader.com", false);
                    c.WriteLine("Breach image by sbc on PicsArt.com", false);
                    break;
                default:
                    c.WriteLine("'" + startMenuCommand + "' is not recognized. Type 'help' for a list of available commands.");
                    break;
            }
            return true; // if the function hasn't returned false yet, the user hasn't exited 
        }
    }
}
