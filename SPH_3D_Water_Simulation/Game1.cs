using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using OpenCL.Net;
using OpenCL.Net.Extensions;
using Environment = System.Environment;
using FormClosingEventArgs = System.Windows.Forms.FormClosingEventArgs;
using Form = System.Windows.Forms.Form;

namespace SPH_3D_Water_Simulation
{

    public class ExtendedDatatypes
    {
        public struct int3
        {
            public int X, Y, Z;

            public int3(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
            public static int3 operator -(int3 i1, int3 i2)
            {
                return new int3(i1.X - i2.X, i1.Y - i2.Y, i1.Z - i2.Z);
            }
            public static int3 operator +(int3 i1, int3 i2)
            {
                return new int3(i1.X + i2.X, i1.Y + i2.Y, i1.Z + i2.Z);
            }
            public static int3 operator *(int3 i1, int3 i2)
            {
                return new int3(i1.X * i2.X, i1.Y * i2.Y, i1.Z * i2.Z);
            }
            public static int3 operator /(int3 i1, int3 i2)
            {
                return new int3(i1.X / i2.X, i1.Y / i2.Y, i1.Z / i2.Z);
            }
            public static int3 operator %(int3 i1, int3 i2)
            {
                return new int3(i1.X % i2.X, i1.Y % i2.Y, i1.Z % i2.Z);
            }
            public static bool operator ==(int3 i1, int3 i2)
            {
                return (i1.X == i2.X && i1.Y == i2.Y && i1.Z == i2.Z);
            }
            public static bool operator !=(int3 i1, int3 i2)
            {
                return !(i1.X == i2.X && i1.Y == i2.Y && i1.Z == i2.Z);
            }

            public override string ToString()
            {
                return "{" + X + ", " + Y + ", " + Z + "}";
            }
        }
        public struct int2
        {
            public int X, Y;

            public int2(int x, int y)
            {
                X = x;
                Y = y;
            }
            public static int2 operator -(int2 i1, int2 i2)
            {
                return new int2(i1.X - i2.X, i1.Y - i2.Y);
            }
            public static int2 operator +(int2 i1, int2 i2)
            {
                return new int2(i1.X + i2.X, i1.Y + i2.Y);
            }
            public static int2 operator *(int2 i1, int2 i2)
            {
                return new int2(i1.X * i2.X, i1.Y * i2.Y);
            }
            public static int2 operator /(int2 i1, int2 i2)
            {
                return new int2(i1.X / i2.X, i1.Y / i2.Y);
            }
            public static int2 operator /(int2 i1, int i2)
            {
                return new int2(i1.X / i2, i1.Y / i2);
            }
            public static int2 operator %(int2 i1, int2 i2)
            {
                return new int2(i1.X % i2.X, i1.Y % i2.Y);
            }
            public static bool operator ==(int2 i1, int2 i2)
            {
                return (i1.X == i2.X && i1.Y == i2.Y);
            }
            public static bool operator !=(int2 i1, int2 i2)
            {
                return !(i1.X == i2.X && i1.Y == i2.Y);
            }

            public override string ToString()
            {
                return "{" + X + ", " + Y + "}";
            }
        }
        public struct short3
        {
            public short X, Y, Z;

            public short3(short x, short y, short z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
    }

    public class Game1 : Game
    {
        public struct InstanceInfo
        {
            public Vector4 World;
            public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0)
            );
        };

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private SpriteFont font;
        private Event OpenCL_event;
        public static Random r = new Random();
        private Model sphere;

        #region Input

        public static KeyboardState oldkeyboardstate, keyboardstate;
        public static MouseState oldmousestate, mousestate;
        private Vector2 cameramousepos, mouserotationbuffer;
        private System.Drawing.Point mousepointpos;

        #endregion

        #region Camera

        public static Vector3 camerapos = new Vector3(200, 200, -500), camerarichtung;
        public static Matrix camview, camworld, camprojection;
        private BasicEffect cameraeffect;
        private Vector3 rotation;
        private bool camerabewegen;
        private float cameraspeed = 0.1f;

        #endregion

        private Effect particle_effect;
        private VertexBuffer particlevertexBuffer;
        private DynamicVertexBuffer instanceBuffer;
        private VertexBufferBinding[] vertexbinding;
        private IndexBuffer indexbuffer;
        private InstanceInfo[] instances;

        short[] particelgridID;
        byte[] particelgrid_curentanz;
        private float[] particlepos = new float[MAX_PARTICLES * 3];
        private float[] particlepos_buffer = new float[MAX_PARTICLES * 3];
        private float[] particlepos_prev = new float[MAX_PARTICLES * 3];
        private float[] particlespeed = new float[MAX_PARTICLES * 3];
        private float[] particlerho = new float[MAX_PARTICLES];
        private float[] particlerho_near = new float[MAX_PARTICLES];
        private float[] particlep = new float[MAX_PARTICLES];
        private float[] particlep_near = new float[MAX_PARTICLES];
        private bool[] particle_used = new bool[MAX_PARTICLES];
        byte[] particle_gridcoox = new byte[MAX_PARTICLES];
        byte[] particle_gridcooy = new byte[MAX_PARTICLES];
        byte[] particle_gridcooz = new byte[MAX_PARTICLES];
        int[] particle_neighbouranz = new int[MAX_PARTICLES];
        int[] particle_neighbourID = new int[MAX_PARTICLES * 2000];

        //G P U
        private Mem GPU_particlepos, GPU_particlepos_prev, GPU_particlespeed, GPU_particlerho, GPU_particlerho_near, GPU_particlep, GPU_particlep_near, GPU_particlepos_buffer;
        private Mem GPU_particlegridID, GPU_particlegrid_curentanz, GPU_particle_gridcoox, GPU_particle_gridcooy, GPU_particle_gridcooz, GPU_particle_neighbouranz, GPU_particle_neighbourID, testmem;

        private Context context;
        private Kernel testkernel, neighbourkernel, displacementkernel;
        private CommandQueue cmdqueue;
        private OpenCL.Net.Program program;
        private string program_string;

        //Simulation Settings
        Vector3 G = new Vector3(0.0f, -1000.0f, 0.0f); // external (gravitational) forces
        float REST_DENS = 170.0f; // rest density
        const float GAS_CONST = 2500.0f; // const for equation of state
        const float H = 35.0f; // kernel radius
        const float onedivH = 1 / H;
        const float HSQ = H * H; // radius^2 for optimization
        const float MASS = 65.0f; // assume all particles have the same mass
        const float VISC = 250.0f; // viscosity constant
        const float DT = 0.005f; // integration timestep
        private const float onedivDT = 1 / DT;
        private const float DTSQR = DT * DT;
        private float DTSQRhalf = DTSQR * 0.5f;
        private float DThalf = DT * 0.5f;

        public static float kStiffness = 4.5f;
        public static float kStiffnessNear = 260.0f;
        public static float kLinearViscocity = 0.065f;
        public static float kQuadraticViscocity = 0.0155f;
        private float height;

        private const int MAX_PARTICLES = 300 * 300;
        private int currentparticleanz = 0;
        private int particleradius = 2;

        private int3 gridsize;
        private Vector3 gridboundary;
        private float gridwidth;
        private int griddepth;
        private int gridoffset = 2;

        public static int Screenwidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        public static int Screenheight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        private bool IsPause = false, IsDebug = false;
        private float memorytime, kerneltime, cputime;
        private int firstrun = 0;
        private float[] averagecomputingtime = new float[1000];
        private int currenttimeindex = 0;
        private decimal averagekerneltime = 0;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            var form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(this.Window.Handle);
            form.Location = new System.Drawing.Point(0, 0);
            graphics.IsFullScreen = false;
            this.IsFixedTimeStep = false;
            //TargetElapsedTime = new TimeSpan((long)(TimeSpan.TicksPerMillisecond * 11.94444));
            Window.IsBorderless = true;
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.PreferredBackBufferHeight = Screenheight;
            graphics.PreferredBackBufferWidth = Screenwidth;
            graphics.ApplyChanges();

            Form f = Form.FromHandle(Window.Handle) as Form;
            if (f != null) { f.FormClosing += f_FormClosing; }
            base.Initialize();
        }

        #region Functions

        public static void DrawMesh(BasicEffect basiceffect, Model model, Matrix matrix)
        {
            Matrix[] transformations = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transformations);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    //effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.EmissiveColor = basiceffect.EmissiveColor;
                    effect.LightingEnabled = basiceffect.LightingEnabled;
                    if (effect.LightingEnabled == true)
                    {
                        effect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight0.Direction = new Vector3(1, -1, 0);
                        effect.DirectionalLight0.DiffuseColor = Color.LightGoldenrodYellow.ToVector3() * 0.2f;
                        effect.DirectionalLight0.SpecularColor = Color.LightGoldenrodYellow.ToVector3() * 1;
                    }

                    effect.World = transformations[mesh.ParentBone.Index] * matrix;
                    effect.View = basiceffect.View;
                    effect.Projection = basiceffect.Projection;
                }

                mesh.Draw();
            }
        }

        public static Effect line_effect;
        public static void DrawLine3d(GraphicsDevice graphicsdevice, Vector3 pos1, Vector3 pos2)
        {
            DrawLine3d(graphicsdevice, pos1, pos2, Color.White, Color.White);
        }
        public static void DrawLine3d(GraphicsDevice graphicsdevice, Vector3 pos1, Vector3 pos2, Color color1, Color color2)
        {
            var vertices = new[] { new VertexPositionColor(pos1, color1), new VertexPositionColor(pos2, color2) };
            line_effect.CurrentTechnique.Passes[0].Apply();
            graphicsdevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
        }
        public static void DrawLine3d(GraphicsDevice graphicsdevice, Vector3 pos1, Vector3 pos2, Color color)
        {
            DrawLine3d(graphicsdevice, pos1, pos2, color, color);
        }

        private void f_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Exit();
            Environment.Exit(0);
            Thread.Sleep(100);
            base.Exit();
        }

        #region OPEN CL

        OpenCL.Net.Program OpenCL_CompileProgram(Context context, Device device, string Sourcecode, out string errorstring)
        {
            ErrorCode errorcode;
            OpenCL.Net.Program program;
            program = Cl.CreateProgramWithSource(context, 1, new[] { Sourcecode }, new[] { (IntPtr)Sourcecode.Length }, out errorcode);
            if (errorcode != ErrorCode.Success)
                Console.WriteLine(errorcode.ToString());
            //-cl-opt-disable
            //-cl-mad-enable
            //-cl-fast-relaxed-math
            //-cl-strict-aliasing // BEST
            errorcode = Cl.BuildProgram(program, 0, null, "-cl-strict-aliasing", null, IntPtr.Zero);
            errorstring = "";
            if (Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Status, out errorcode).CastTo<BuildStatus>() != BuildStatus.Success)
            {

                if (errorcode != ErrorCode.Success) // Couldn´t get Programm Build Info
                    errorstring += "ERROR: " + "Cl.GetProgramBuildInfo" + " (" + errorcode.ToString() + ")" + "\r\n";
                errorstring += "Cl.GetProgramBuildInfo != Success" + "\r\n";
                errorstring += Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Log, out errorcode); // Printing Log
            }
            else
                errorstring = "SUCCESS";
            return program;
        }

        ErrorCode OpenCL_RunKernel(Kernel kernel, CommandQueue queue, int size)
        {
            return Cl.EnqueueNDRangeKernel(queue, kernel, 1, null, new IntPtr[] { new IntPtr(size), }, null, 0, null, out OpenCL_event);
        }

        ErrorCode OpenCL_ReadGPUMemory<T>(CommandQueue queue, Mem GPUMemory, T[] data) where T : struct
        {
            int size = Marshal.SizeOf<T>() * data.Length;
            return Cl.EnqueueReadBuffer(queue, GPUMemory, Bool.True, IntPtr.Zero, new IntPtr(size), data, 0, null, out OpenCL_event);
        }

        Mem OpenCL_CreateGPUBuffer<T>(Context context, int length, out ErrorCode error) where T : struct
        {
            int size = Marshal.SizeOf<T>() * length;
            return (Mem)Cl.CreateBuffer(context, MemFlags.ReadWrite, size, out error);
        }
        ErrorCode OpenCL_WriteGPUMemory<T>(CommandQueue queue, Mem GPUMemory, T[] data) where T : struct
        {
            int size = Marshal.SizeOf<T>() * data.Length;
            return Cl.EnqueueWriteBuffer(queue, GPUMemory, Bool.True, IntPtr.Zero, new IntPtr(size), data, 0, null, out OpenCL_event);

        }

        Context OpenCL_CreateContext(int numdevices, Device[] devices, out ErrorCode error)
        {
            return Cl.CreateContext(null, 1, devices, null, IntPtr.Zero, out error);
        }

        #endregion

        #endregion

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            line_effect = Content.Load<Effect>("line_effect");

            font = Content.Load<SpriteFont>("font");
            cameraeffect = new BasicEffect(GraphicsDevice);
            cameraeffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(80), GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000000);
            cameraeffect.World = Matrix.Identity;
            cameraeffect.LightingEnabled = true;
            cameraeffect.EmissiveColor = new Vector3(1, 0, 0);
            sphere = Content.Load<Model>("sphere");

            // Loading Simulation Settings
            griddepth = 255;
            gridwidth = H / 1.2f;
            gridsize = new int3(20, 40, 20);
            gridboundary = new Vector3(gridsize.x - 4, gridsize.y - 4, gridsize.z - 4) * gridwidth;
            particelgridID = new short[(gridsize.x) * (gridsize.y) * (gridsize.z) * griddepth];
            particelgrid_curentanz = new byte[(gridsize.x) * (gridsize.y) * (gridsize.z)];
            height = 0;

            program_string = File.ReadAllText("main.c");

            // Inizialising GPU and Kernel
            ErrorCode err;
            string errorstring;
            Platform[] platforms = Cl.GetPlatformIDs(out err); // Getting all Platforms
            Console.WriteLine("Length:" + platforms.Length);
            Device[] devices = Cl.GetDeviceIDs(platforms[0], DeviceType.Gpu, out err); // Getting all devices
            Console.WriteLine("Length_devices0:" + devices.Length);
            context = OpenCL_CreateContext(1, devices, out err);
            cmdqueue = Cl.CreateCommandQueue(context, devices[0], CommandQueueProperties.None, out err);
            program = OpenCL_CompileProgram(context, devices[0], program_string, out errorstring);
            if (errorstring != "SUCCESS")
            {
                Console.WriteLine(errorstring);
                throw new System.InvalidOperationException("Error during Building the Program");
            }
            else
                Console.WriteLine("Building program succeeded");

            neighbourkernel = Cl.CreateKernel(program, "Getneighbours_calcviscosity", out err);
            displacementkernel = Cl.CreateKernel(program, "GetDensityPressureDisplacement", out err);

            #region Inizialising GPU Memory

            GPU_particlepos = OpenCL_CreateGPUBuffer<float>(context, particlepos.Length, out err);
            GPU_particlepos_buffer = OpenCL_CreateGPUBuffer<float>(context, particlepos_buffer.Length, out err);
            GPU_particlepos_prev = OpenCL_CreateGPUBuffer<float>(context, particlepos_prev.Length, out err);
            GPU_particlespeed = OpenCL_CreateGPUBuffer<float>(context, particlespeed.Length, out err);
            GPU_particlerho = OpenCL_CreateGPUBuffer<float>(context, particlerho.Length, out err);
            GPU_particlerho_near = OpenCL_CreateGPUBuffer<float>(context, particlerho_near.Length, out err);
            GPU_particlep = OpenCL_CreateGPUBuffer<float>(context, particlep.Length, out err);
            GPU_particlep_near = OpenCL_CreateGPUBuffer<float>(context, particlep_near.Length, out err);

            GPU_particlegrid_curentanz = OpenCL_CreateGPUBuffer<int>(context, particelgrid_curentanz.Length, out err);
            GPU_particlegridID = OpenCL_CreateGPUBuffer<int>(context, particelgridID.Length, out err);
            GPU_particle_gridcoox = OpenCL_CreateGPUBuffer<int>(context, particle_gridcoox.Length, out err);
            GPU_particle_gridcooy = OpenCL_CreateGPUBuffer<int>(context, particle_gridcooy.Length, out err);
            GPU_particle_gridcooz = OpenCL_CreateGPUBuffer<int>(context, particle_gridcooz.Length, out err);
            GPU_particle_neighbouranz = OpenCL_CreateGPUBuffer<int>(context, particle_neighbouranz.Length, out err);
            GPU_particle_neighbourID = OpenCL_CreateGPUBuffer<int>(context, particle_neighbourID.Length, out err);

            #endregion

            #region Writing GPU Memory

            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos, particlepos);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos_prev, particlepos_prev);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlespeed, particlespeed);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_neighbouranz, particle_neighbouranz);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_neighbourID, particle_neighbourID);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_gridcoox, particle_gridcoox);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_gridcooy, particle_gridcooy);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_gridcooz, particle_gridcooz);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlegrid_curentanz, particelgrid_curentanz);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlegridID, particelgridID);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlerho, particlerho);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlerho_near, particlerho_near);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlep, particlep);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlep_near, particlep_near);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos_buffer, particlepos_buffer);

            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlerho, particlerho);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlerho_near, particlerho_near);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlep, particlep);
            OpenCL_WriteGPUMemory(cmdqueue, GPU_particlep_near, particlep_near);

            #endregion

            #region Setting Kernel Arguments

            Cl.SetKernelArg(neighbourkernel, 3, GPU_particle_neighbouranz);
            Cl.SetKernelArg(neighbourkernel, 4, GPU_particle_neighbourID);
            Cl.SetKernelArg(neighbourkernel, 0, GPU_particlepos);
            Cl.SetKernelArg(neighbourkernel, 1, GPU_particlepos_prev);
            Cl.SetKernelArg(neighbourkernel, 2, GPU_particlespeed);
            Cl.SetKernelArg(neighbourkernel, 5, GPU_particle_gridcoox);
            Cl.SetKernelArg(neighbourkernel, 6, GPU_particle_gridcooy);
            Cl.SetKernelArg(neighbourkernel, 7, GPU_particle_gridcooz);
            Cl.SetKernelArg(neighbourkernel, 8, GPU_particlegrid_curentanz);
            Cl.SetKernelArg(neighbourkernel, 9, GPU_particlegridID);
            Cl.SetKernelArg(neighbourkernel, 10, gridsize.x);
            Cl.SetKernelArg(neighbourkernel, 11, gridsize.y);
            Cl.SetKernelArg(neighbourkernel, 12, kLinearViscocity);
            Cl.SetKernelArg(neighbourkernel, 13, kQuadraticViscocity);

            Cl.SetKernelArg(displacementkernel, 0, GPU_particlepos);
            Cl.SetKernelArg(displacementkernel, 1, GPU_particlepos_prev);
            Cl.SetKernelArg(displacementkernel, 2, GPU_particlerho);
            Cl.SetKernelArg(displacementkernel, 3, GPU_particlerho_near);
            Cl.SetKernelArg(displacementkernel, 4, GPU_particlep);
            Cl.SetKernelArg(displacementkernel, 5, GPU_particlep_near);
            Cl.SetKernelArg(displacementkernel, 6, GPU_particle_neighbouranz);
            Cl.SetKernelArg(displacementkernel, 7, GPU_particle_neighbourID);
            Cl.SetKernelArg(displacementkernel, 8, REST_DENS);
            Cl.SetKernelArg(displacementkernel, 9, kStiffness);
            Cl.SetKernelArg(displacementkernel, 10, kStiffnessNear);
            Cl.SetKernelArg(displacementkernel, 11, GPU_particlespeed);
            Cl.SetKernelArg(displacementkernel, 12, height);
            Cl.SetKernelArg(displacementkernel, 13, G.X);
            Cl.SetKernelArg(displacementkernel, 14, G.Y);
            Cl.SetKernelArg(displacementkernel, 15, G.Z);

            #endregion

            int xsize = 20;
            int ysize = 30;
            int zsize = 20;
            int xysize = xsize * ysize;

            float dis = 4.65f;

            // Particles for Dam Break Simulation
            for (int i = 0; i < xsize; i++)
            {
                for (int j = 0; j < ysize; j++)
                {
                    for (int k = 0; k < zsize; k++)
                    {
                        currentparticleanz += 1;
                        particle_used[i + xsize * j + k * xysize] = true;
                        particlepos[(i + xsize * j + k * xysize) * 3] = particlepos_prev[(i + xsize * j + k * xysize) * 3] = particleradius * dis * i + r.Next(0, 1000) / 10000.0f + 110;
                        particlepos[(i + xsize * j + k * xysize) * 3 + 1] = particlepos_prev[(i + xsize * j + k * xysize) * 3 + 1] = particleradius * dis * j + 610;
                        particlepos[(i + xsize * j + k * xysize) * 3 + 2] = particlepos_prev[(i + xsize * j + k * xysize) * 3 + 2] = particleradius * dis * k + r.Next(0, 1000) / 10000.0f + 110;
                        particlespeed[(i + xsize * j + k * xysize) * 3] = 0;
                        particlespeed[(i + xsize * j + k * xysize) * 3 + 1] = 0;
                        particlespeed[(i + xsize * j + k * xysize) * 3 + 2] = 0;
                    }
                }
            }

            particlevertexBuffer = sphere.Meshes[0].MeshParts[0].VertexBuffer;
            indexbuffer = sphere.Meshes[0].MeshParts[0].IndexBuffer;
            instances = new InstanceInfo[currentparticleanz];
            for (int i = 0; i < currentparticleanz; ++i)
            {
                instances[i].World = new Vector4(particlepos[i * 3], particlepos[i * 3 + 1], particlepos[i * 3 + 2], 1);
            }
            instanceBuffer = new DynamicVertexBuffer(GraphicsDevice, InstanceInfo.VertexDeclaration, currentparticleanz, BufferUsage.WriteOnly);
            instanceBuffer.SetData(instances);

            vertexbinding = new VertexBufferBinding[2];
            vertexbinding[0] = new VertexBufferBinding(particlevertexBuffer);
            vertexbinding[1] = new VertexBufferBinding(instanceBuffer, 0, 1);
            particle_effect = Content.Load<Effect>("particle_effect");
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            keyboardstate = Keyboard.GetState();
            mousestate = Mouse.GetState();
            var mousePosition = new Vector2(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            #region Updating Camera

            if (keyboardstate.IsKeyUp(Keys.LeftAlt))
            {
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    camerapos.Z -= (float)Math.Sin(rotation.X) * 2 * cameraspeed;
                    camerapos.X += (float)Math.Cos(rotation.X) * 2 * cameraspeed;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    camerapos.Z += (float)Math.Sin(rotation.X) * 2 * cameraspeed;
                    camerapos.X -= (float)Math.Cos(rotation.X) * 2 * cameraspeed;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    camerapos.Y += 2 * cameraspeed;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                {
                    camerapos.Y -= 2 * cameraspeed;
                }
            }

            cameraspeed = Keyboard.GetState().IsKeyDown(Keys.LeftShift) ? 8.5f : 1.3f;

            if (keyboardstate.IsKeyDown(Keys.Tab) && !oldkeyboardstate.IsKeyDown(Keys.Tab))
            {
                Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                camerabewegen = !camerabewegen;
            }

            if (camerabewegen == true && this.IsActive)
            {
                int changed = 0;
                float deltax, deltay;
                deltax = System.Windows.Forms.Cursor.Position.X - cameramousepos.X;
                deltay = System.Windows.Forms.Cursor.Position.Y - cameramousepos.Y;
                mouserotationbuffer.X += 0.004f * deltax;
                mouserotationbuffer.Y += 0.004f * deltay;
                if (mouserotationbuffer.Y < MathHelper.ToRadians(-88))
                {
                    mouserotationbuffer.Y = mouserotationbuffer.Y - (mouserotationbuffer.Y - MathHelper.ToRadians(-88));
                }

                if (mouserotationbuffer.Y > MathHelper.ToRadians(88))
                {
                    mouserotationbuffer.Y = mouserotationbuffer.Y - (mouserotationbuffer.Y - MathHelper.ToRadians(88));
                }

                if (cameramousepos != mousePosition)
                    changed = 1;
                rotation = new Vector3(-mouserotationbuffer.X, -mouserotationbuffer.Y, 0);
                if (changed == 1)
                {
                    System.Windows.Forms.Cursor.Position = mousepointpos;
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed && IsActive)
            {
                if (camerabewegen == false)
                {
                    camerabewegen = true;
                    cameramousepos = mousePosition;
                    mousepointpos.X = (int)mousePosition.X;
                    mousepointpos.Y = (int)mousePosition.Y;
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Released && camerabewegen == true)
            {
                camerabewegen = false;
            }


            Matrix rotationMatrix = Matrix.CreateRotationY(rotation.X); // * Matrix.CreateRotationX(rotationY);
            Vector3 transformedReference = Vector3.TransformNormal(new Vector3(0, 0, 1000), rotationMatrix);
            Vector3 cameraLookat = camerapos + transformedReference;
            camerarichtung.Y = cameraLookat.Y - (float)Math.Sin(-rotation.Y) * Vector3.Distance(camerapos, cameraLookat);
            camerarichtung.X = cameraLookat.X - (cameraLookat.X - camerapos.X) * (float)(1 - Math.Cos(rotation.Y));
            camerarichtung.Z = cameraLookat.Z - (cameraLookat.Z - camerapos.Z) * (float)(1 - Math.Cos(rotation.Y));
            if (keyboardstate.IsKeyUp(Keys.LeftAlt))
            {
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    var camerablickrichtung = camerapos - camerarichtung;
                    camerablickrichtung = camerablickrichtung / camerablickrichtung.Length();
                    camerapos -= camerablickrichtung * 2 * cameraspeed;
                    camerarichtung -= camerablickrichtung * 2 * cameraspeed;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    var camerablickrichtung = camerapos - camerarichtung;
                    camerablickrichtung = camerablickrichtung / camerablickrichtung.Length();
                    camerapos += camerablickrichtung * 2 * cameraspeed;
                    camerarichtung += camerablickrichtung * 2 * cameraspeed;
                }
            }

            cameraeffect.View = Matrix.CreateLookAt(camerapos, camerarichtung, Vector3.Up);
            camworld = cameraeffect.World;
            camview = cameraeffect.View;
            camprojection = cameraeffect.Projection;

            #endregion


            #region INPUT

            if (keyboardstate.IsKeyDown(Keys.P) && oldkeyboardstate.IsKeyUp(Keys.P))
                IsPause ^= true;
            if (keyboardstate.IsKeyDown(Keys.N) && oldkeyboardstate.IsKeyUp(Keys.N))
                IsDebug ^= true;
            if (keyboardstate.IsKeyDown(Keys.Up) && height < gridboundary.Y)
            {
                height += 4;
                Cl.SetKernelArg(displacementkernel, 12, height);
            }

            if (keyboardstate.IsKeyDown(Keys.Down) && height > 0)
            {
                height -= 4;
                Cl.SetKernelArg(displacementkernel, 12, height);
            }

            if (keyboardstate.IsKeyDown(Keys.T))
            {
                kStiffness *= 1.025f;
                Cl.SetKernelArg(displacementkernel, 9, kStiffness);
            }

            if (keyboardstate.IsKeyDown(Keys.G))
            {
                kStiffness /= 1.025f;
                Cl.SetKernelArg(displacementkernel, 9, kStiffness);
            }

            if (keyboardstate.IsKeyDown(Keys.Z))
            {
                kStiffnessNear *= 1.05f;
                Cl.SetKernelArg(displacementkernel, 10, kStiffnessNear);
            }

            if (keyboardstate.IsKeyDown(Keys.H))
            {
                kStiffnessNear /= 1.05f;
                Cl.SetKernelArg(displacementkernel, 10, kStiffnessNear);
            }

            if (keyboardstate.IsKeyDown(Keys.U))
            {
                REST_DENS *= 1.05f;
                Cl.SetKernelArg(displacementkernel, 8, REST_DENS);
            }

            if (keyboardstate.IsKeyDown(Keys.J))
            {
                REST_DENS /= 1.05f;
                Cl.SetKernelArg(displacementkernel, 8, REST_DENS);
            }

            if (keyboardstate.IsKeyDown(Keys.I))
            {
                kLinearViscocity *= 1.05f;
                Cl.SetKernelArg(neighbourkernel, 12, kLinearViscocity);
            }

            if (keyboardstate.IsKeyDown(Keys.K))
            {
                kLinearViscocity /= 1.05f;
                Cl.SetKernelArg(neighbourkernel, 12, kLinearViscocity);
            }

            if (keyboardstate.IsKeyDown(Keys.O))
            {
                kQuadraticViscocity *= 1.05f;
                Cl.SetKernelArg(neighbourkernel, 13, kQuadraticViscocity);
            }

            if (keyboardstate.IsKeyDown(Keys.L))
            {
                kQuadraticViscocity /= 1.05f;
                Cl.SetKernelArg(neighbourkernel, 13, kQuadraticViscocity);
            }

            if (keyboardstate.IsKeyDown(Keys.LeftAlt))
            {
                if (keyboardstate.IsKeyDown(Keys.W))
                {
                    G.Y += 10;
                    Cl.SetKernelArg(displacementkernel, 14, G.Y);
                }

                if (keyboardstate.IsKeyDown(Keys.S))
                {
                    G.Y -= 10;
                    Cl.SetKernelArg(displacementkernel, 14, G.Y);
                }

                if (keyboardstate.IsKeyDown(Keys.A))
                {
                    G.X += 10;
                    Cl.SetKernelArg(displacementkernel, 13, G.X);
                }

                if (keyboardstate.IsKeyDown(Keys.D))
                {
                    G.X -= 10;
                    Cl.SetKernelArg(displacementkernel, 13, G.X);
                }
            }

            #endregion

            if (!IsPause)
            {

                // Sorting Particles into the grid
                Stopwatch watch3 = new Stopwatch();
                watch3.Start();
                for (int x = 0; x < gridsize.x; ++x)
                {
                    for (int y = 0; y < gridsize.y; ++y)
                    {
                        for (int z = 0; z < gridsize.z; ++z)
                        {
                            particelgrid_curentanz[x + y * gridsize.x + z * gridsize.x * gridsize.y] = 0;
                        }
                    }
                }

                int timer = 0;
                for (int i = 0; i < MAX_PARTICLES; i++)
                {
                    int i3 = i * 3;
                    if (particle_used[i])
                    {
                        /*if (particlepos[i3] <= -(gridoffset - 1) * gridwidth)
                            particlepos[i3] = particlepos_prev[i3] = -(gridoffset - 1) * gridwidth + 0.1f;
                        if (particlepos[i3 + 1] <= -(gridoffset - 1) * gridwidth)
                            particlepos[i3 + 1] = particlepos_prev[i3 + 1] = -(gridoffset - 1) * gridwidth + 0.1f;
                        if (particlepos[i3] >= gridwidth * (gridsizeX - 2 * (gridoffset - 1) - 1))
                            particlepos[i3] = particlepos_prev[i2] = gridwidth * (gridsizeX - 2 * (gridoffset - 1) - 1) - 0.1f;
                        if (particlepos[i3 + 1] >= gridwidth * (gridsizeY - 2 * (gridoffset - 1) - 1))
                            particlepos[i3 + 1] = particlepos_prev[i3 + 1] = gridwidth * (gridsize.y - 2 * (gridoffset - 1) - 1) - 0.1f;*/

                        int xindex = (int)(particlepos[i3] / gridwidth + gridoffset);
                        int yindex = (int)(particlepos[i3 + 1] / gridwidth + gridoffset);
                        int zindex = (int)(particlepos[i3 + 2] / gridwidth + gridoffset);
                        if (particelgrid_curentanz[xindex + yindex * (gridsize.x) + zindex * (gridsize.x) * (gridsize.y)] < griddepth)
                        {
                            particelgridID[(xindex + yindex * (gridsize.x) + zindex * (gridsize.x) * (gridsize.y)) * griddepth + particelgrid_curentanz[xindex + yindex * (gridsize.x) + zindex * (gridsize.x) * (gridsize.y)]] = (short)i;
                            particle_gridcoox[i] = (byte)xindex;
                            particle_gridcooy[i] = (byte)yindex;
                            particle_gridcooz[i] = (byte)zindex;
                            particelgrid_curentanz[xindex + yindex * gridsize.x + zindex * gridsize.x * gridsize.y]++;
                            timer++;
                            if (xindex == 0 || yindex == 0 || zindex == 0 || xindex == gridsize.x - 1 || yindex == gridsize.y - 1 || zindex == gridsize.z - 1)
                            {
                                int x = 0;
                            }
                        }

                    }
                }

                watch3.Stop();
                cputime = watch3.ElapsedTicks / ((float)Stopwatch.Frequency / 1000.0f);
                // Adding Gravity
                for (int i = 0; i < currentparticleanz; i++)
                {
                    if (particle_used[i] == true)
                    {
                        //particlespeed[i * 2] += DT * G.X;
                        //particlespeed[i * 2 + 1] += DT * G.Y;
                    }
                }

                if (firstrun == 1)
                {

                    OpenCL_ReadGPUMemory(cmdqueue, GPU_particlerho, particlerho);
                    Cl.ReleaseEvent(OpenCL_event);
                }

                //Copying Data to GPU Memory
                ErrorCode err;
                Stopwatch watch = new Stopwatch();
                watch.Start();
                ////OpenCL_WriteGPUMemory(cmdqueue, GPU_particlespeed, particlespeed);
                //Cl.ReleaseEvent(OpenCL_event);
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_gridcoox, particle_gridcoox);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_gridcooy, particle_gridcooy);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particle_gridcooz, particle_gridcooz);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particlegridID, particelgridID);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particlegrid_curentanz, particelgrid_curentanz);
                Cl.ReleaseEvent(OpenCL_event);
                watch.Stop();


                firstrun = 1;

                OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos, particlepos);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos_prev, particlepos_prev);
                Cl.ReleaseEvent(OpenCL_event);

                //Setting ViscosityKernel Arguments


                // Running ViscosityKernel 
                Stopwatch watch2 = new Stopwatch();
                watch2.Start();
                OpenCL_RunKernel(neighbourkernel, cmdqueue, currentparticleanz);
                Cl.Finish(cmdqueue);
                Cl.ReleaseEvent(OpenCL_event);
                watch2.Stop();

                watch.Start();
                OpenCL_ReadGPUMemory(cmdqueue, GPU_particlepos, particlepos);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_ReadGPUMemory(cmdqueue, GPU_particlespeed, particlespeed);
                Cl.ReleaseEvent(OpenCL_event);
                //OpenCL_ReadGPUMemory(cmdqueue, GPU_particle_neighbouranz, particle_neighbouranz);
                //Cl.ReleaseEvent(OpenCL_event);
                watch.Stop();
                for (int i = 0; i < currentparticleanz; i++)
                {

                    int i3 = i * 3;
                    //particlepos_prev[i2] = particlepos[i2];
                    //particlepos_prev[i2 + 1] = particlepos[i2 + 1];
                    particlepos[i3] += particlespeed[i3] * DT;
                    particlepos[i3 + 1] += particlespeed[i3 + 1] * DT;
                    particlepos[i3 + 2] += particlespeed[i3 + 2] * DT;

                }

                watch.Start();
                OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos, particlepos);
                Cl.ReleaseEvent(OpenCL_event);
                //OpenCL_WriteGPUMemory(cmdqueue, GPU_particlepos_prev, particlepos_prev);
                //Cl.ReleaseEvent(OpenCL_event);
                //OpenCL_WriteGPUMemory(cmdqueue, GPU_particlespeed, particlespeed);
                //Cl.ReleaseEvent(OpenCL_event);
                watch.Stop();
                //Setting DisplacementKernel Arguments

                //OpenCL_WriteGPUMemory(cmdqueue, GPU_particlegrid_curentanz, particelgrid_curentanz);
                //Cl.ReleaseEvent(OpenCL_event);

                // Running DisplacementKernel
                watch2.Start();
                OpenCL_RunKernel(displacementkernel, cmdqueue, currentparticleanz);
                Cl.Finish(cmdqueue);
                Cl.ReleaseEvent(OpenCL_event);
                watch2.Stop();
                kerneltime = watch2.ElapsedTicks / ((float)Stopwatch.Frequency / 1000.0f);

                OpenCL_ReadGPUMemory(cmdqueue, GPU_particlepos, particlepos);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_ReadGPUMemory(cmdqueue, GPU_particlepos_prev, particlepos_prev);
                Cl.ReleaseEvent(OpenCL_event);

                // Reading Data
                //OpenCL_ReadGPUMemory(cmdqueue, GPU_particlepos, particlepos);
                //Cl.ReleaseEvent(OpenCL_event);
                //OpenCL_ReadGPUMemory(cmdqueue, GPU_particlerho, particlerho);
                //Cl.ReleaseEvent(OpenCL_event);
                memorytime = watch.ElapsedTicks / ((float)Stopwatch.Frequency / 1000.0f);
                if (currenttimeindex < 1000)
                {
                    averagecomputingtime[currenttimeindex] = kerneltime;
                    currenttimeindex++;
                }
                else if (currenttimeindex == 1000)
                {
                    currenttimeindex++;
                    for (int i = 0; i < 1000; i++)
                    {
                        averagekerneltime += (decimal)averagecomputingtime[i];
                    }

                    averagekerneltime /= 1000m;
                }
                //OpenCL_ReadGPUMemory(cmdqueue, GPU_particlespeed, particlespeed);
                //OpenCL_ReadGPUMemory(cmdqueue, GPU_particle_neighbouranz, particle_neighbouranz);

                for (int i = 0; i < currentparticleanz; i++)
                {
                    int i3 = i * 3;
                    //particlespeed[i2] = (particlepos[i2] - particlepos_prev[i2])  * onedivDT;
                    //particlespeed[i2 + 1] = (particlepos[i2 + 1] - particlepos_prev[i2 + 1])  * onedivDT;
                }
            }

            for (int i = 0; i < currentparticleanz; ++i)
            {
                instances[i].World = new Vector4(particlepos[i * 3], particlepos[i * 3 + 1], particlepos[i * 3 + 2], 1);
            }
            instanceBuffer.SetData(instances);

            base.Update(gameTime);
            oldkeyboardstate = keyboardstate;
            oldmousestate = mousestate;
        }

        public void BeginRender3D()
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            line_effect.Parameters["World"].SetValue(camworld);
            line_effect.Parameters["View"].SetValue(camview);
            line_effect.Parameters["Projection"].SetValue(camprojection);
        }

        protected override void Draw(GameTime gameTime)
        {
            BeginRender3D();
            GraphicsDevice.Clear(Color.DimGray);

            DrawLine3d(GraphicsDevice, Vector3.Zero, new Vector3(gridboundary.X, 0, 0), Color.White);
            DrawLine3d(GraphicsDevice, Vector3.Zero, new Vector3(0, gridboundary.Y, 0), Color.White);
            DrawLine3d(GraphicsDevice, Vector3.Zero, new Vector3(0, 0, gridboundary.Z), Color.White);
            DrawLine3d(GraphicsDevice, gridboundary, new Vector3(gridboundary.X, gridboundary.Y, 0), Color.White);
            DrawLine3d(GraphicsDevice, gridboundary, new Vector3(0, gridboundary.Y, gridboundary.Z), Color.White);
            DrawLine3d(GraphicsDevice, gridboundary, new Vector3(gridboundary.X, 0, gridboundary.Z), Color.White);

            DrawLine3d(GraphicsDevice, new Vector3(0, 0, gridboundary.Z), new Vector3(0, gridboundary.Y, gridboundary.Z), Color.White);
            DrawLine3d(GraphicsDevice, new Vector3(gridboundary.X, 0, 0), new Vector3(gridboundary.X, gridboundary.Y, 0), Color.White);
            DrawLine3d(GraphicsDevice, new Vector3(0, 0, gridboundary.Z), new Vector3(gridboundary.X, 0, gridboundary.Z), Color.White);
            DrawLine3d(GraphicsDevice, new Vector3(0, gridboundary.Y, 0), new Vector3(gridboundary.X, gridboundary.Y, 0), Color.White);
            DrawLine3d(GraphicsDevice, new Vector3(gridboundary.X, 0, 0), new Vector3(gridboundary.X, 0, gridboundary.Z), Color.White);
            DrawLine3d(GraphicsDevice, new Vector3(0, gridboundary.Y, 0), new Vector3(0, gridboundary.Y, gridboundary.Z), Color.White);


            /*for (int i = 0; i < currentparticleanz; i++)
	        {
		        DrawMesh(cameraeffect, sphere, Matrix.CreateScale(0.03f) * Matrix.CreateTranslation(new Vector3(particlepos[i * 3], particlepos[i * 3 + 1], particlepos[i * 3 + 2])));
            }*/

            particle_effect.Parameters["WVP"].SetValue(camview * camprojection);
            //particle_effect.Parameters["View"].SetValue(camview);
            //particle_effect.Parameters["Projection"].SetValue(camprojection);
            particle_effect.CurrentTechnique = particle_effect.Techniques["Instancing"];
            particle_effect.CurrentTechnique.Passes[0].Apply();

            GraphicsDevice.Indices = indexbuffer;
            GraphicsDevice.SetVertexBuffers(vertexbinding);

            GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, indexbuffer.IndexCount, 0, indexbuffer.IndexCount / 3, currentparticleanz);



            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Gravity: " + G.ToString(), new Vector2(50, 50), Color.Red);

            // SPH CONSTANS
            spriteBatch.DrawString(font, "K_Stiffness<T,G>: " + kStiffness.ToString(), new Vector2(50, 80), Color.Red);
            spriteBatch.DrawString(font, "K_Stiffness_Near<Z,H>: " + kStiffnessNear.ToString(), new Vector2(50, 110), Color.Red);
            spriteBatch.DrawString(font, "REAST_DENS<U,J>: " + REST_DENS.ToString(), new Vector2(50, 140), Color.Red);
            spriteBatch.DrawString(font, "K_Linear_Visc<I,K>: " + kLinearViscocity.ToString(), new Vector2(50, 170), Color.Red);
            spriteBatch.DrawString(font, "K_Quadratic_Visc<O,L>: " + kQuadraticViscocity.ToString(), new Vector2(50, 200), Color.Red);

            spriteBatch.DrawString(font, "Memory Time: " + memorytime.ToString(), new Vector2(50, 250), Color.Red);
            spriteBatch.DrawString(font, "Kernel Time: " + kerneltime.ToString(), new Vector2(50, 280), Color.Red);
            spriteBatch.DrawString(font, "CPU Time: " + cputime.ToString(), new Vector2(50, 310), Color.Red);
            spriteBatch.DrawString(font, "Average Kernel Time: " + averagekerneltime.ToString(), new Vector2(50, 340), Color.Red);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
