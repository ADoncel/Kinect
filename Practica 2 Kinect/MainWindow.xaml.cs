//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Diagnostics;
    using System.Threading;

    public enum Posture
    {
        None,
        Inicio,
        Fase1,
        Fase2,
        Transcurso1,
        Transcurso2,
        Final
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PostureDetectionNumber = 10;
        int accumulator = 0;
        Posture postureStart = Posture.Inicio;
        Posture postureInicial = Posture.None;
        float error;

        // Puntos de union y Pens con los que vamos a pintar los huesos del cuerpo segun el movimiento
        /*
         * wrist --> muñeca
         * elbow --> codo
         * shoulder --> hombro
         * hip --> cadera
         * knee --> rodilla
         * ankle --> tobillo
        */
        Joint wristR, elbowR, shoulderR, wristL, elbowL, shoulderL; //Parte superior
        Joint hipC, kneeR, ankleR, kneeL, ankleL, ankleIR, ankleIL;                   //Parte inferior
        private readonly Pen penFin = new Pen(Brushes.Green, 6);
        private readonly Pen penProceso = new Pen(Brushes.Yellow, 6);
        private readonly Pen penInicio = new Pen(Brushes.Blue, 6);
        private readonly Pen penError = new Pen(Brushes.Red, 6);

        // Booleanos para controlar la posicion y pintar los huesos de distinto color
        private bool reposo = false;
        private bool proceso1 = false;
        private bool proceso2 = false;
        private bool fin1 = false;
        private bool fin2 = false;
        private int contError1 = 0, contError2 = 0;
        private int numMov = 1;
        private bool subida = false;

        /*** RELOJ *** /
        Stopwatch clock = new Stopwatch();
        TimeSpan time = new TimeSpan();
        /*************/

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.White, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.RosyBrown, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.ColorImage.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                                                 this.colorPixels,
                                                 this.colorBitmap.PixelWidth * sizeof(int),
                                                 0);
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }

            foreach (Skeleton bones in skeletons)
            {
                // Guardamos los puntos de union que nos interesan para el movimiento
                if (bones.TrackingState == SkeletonTrackingState.Tracked)
                {
                    shoulderR = bones.Joints[JointType.ShoulderRight];
                    shoulderL = bones.Joints[JointType.ShoulderLeft];
                    wristR = bones.Joints[JointType.WristRight];
                    wristL = bones.Joints[JointType.WristLeft];
                    elbowR = bones.Joints[JointType.ElbowRight];
                    elbowL = bones.Joints[JointType.ElbowLeft];
                    
                    hipC = bones.Joints[JointType.HipCenter];
                    kneeR = bones.Joints[JointType.KneeRight];
                    kneeL = bones.Joints[JointType.KneeLeft];
                    ankleR = bones.Joints[JointType.AnkleRight];
                    ankleL = bones.Joints[JointType.AnkleRight];
                }
            }

            int aux1 = (int)(error * 100);
            string mensaje = "" + aux1 + "%";
            this.muestraError.Text = mensaje;
            movRestantes.Text = numMov + "";

            // Llamada a las comprobaciones de la posicion del brazo, 
            // para que acceda el punto del hombro debe estar en tracking
            // sino produce errores.
            if (shoulderR.TrackingState == JointTrackingState.Tracked || hipC.TrackingState == JointTrackingState.Tracked)
            {
                if (!subida)
                    comprobarGestosBajada(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, ankleIL, ankleIR, error);
                if (subida)
                    comprobarGestosSubida(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, ankleIL, ankleIR, error);
            }
        }

        //POSICION INICIAL
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/
        // Comprueba si se encuentra en la posicion inicial
        // Mano derecha en cruz y piernas rectas.
        public bool BrazosRectos(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL, float error)
        {
            if (elbowR.Position.Y < shoulderR.Position.Y + 0.06 + (0.03 * error) && elbowR.Position.Y > shoulderR.Position.Y - 0.06 + (0.03 * error) &&
                wristR.Position.Y < shoulderR.Position.Y + 0.1 + (0.05 * error) && wristR.Position.Y > shoulderR.Position.Y - 0.1 + (0.05 * error) &&
                elbowR.Position.Z < shoulderR.Position.Z + 0.06 + (0.03 * error) && elbowR.Position.Z > shoulderR.Position.Z - 0.15 + (0.1 * error) &&
                wristR.Position.Z < shoulderR.Position.Z + 0.06 + (0.03 * error) && wristR.Position.Z > shoulderR.Position.Z - 0.2 + (0.15 * error))
            {
                if (elbowL.Position.Y < shoulderL.Position.Y + 0.06 + (0.03 * error) && elbowL.Position.Y > shoulderL.Position.Y - 0.06 + (0.03 * error) &&
                    wristL.Position.Y < shoulderL.Position.Y + 0.1 + (0.05 * error) && wristL.Position.Y > shoulderL.Position.Y - 0.1 + (0.05 * error) &&
                    elbowL.Position.Z < shoulderL.Position.Z + 0.06 + (0.03 * error) && elbowL.Position.Z > shoulderL.Position.Z - 0.15 + (0.1 * error) &&
                    wristL.Position.Z < shoulderL.Position.Z + 0.06 + (0.03 * error) && wristL.Position.Z > shoulderL.Position.Z - 0.2 + (0.15 * error))
                {
                    return true;
                }
            }
            return false;
        }
        public bool PiernasRectas(Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL)
        {
            if ((kneeL.Position.Z - ankleL.Position.Z) - (hipC.Position.Z - kneeL.Position.Z) > 0.005f - (0.01f * error) &&
                (kneeR.Position.Z - ankleR.Position.Z) - (hipC.Position.Z - kneeR.Position.Z) > 0.005f - (0.01f * error))
            {
                ankleIR = ankleR;
                ankleIL = ankleL;
                return true;
            }
            return false;
        }
        public bool PosInicio(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL, Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL, float error)
        {
            if (BrazosRectos(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error) && PiernasRectas(hipC, kneeR, kneeL, ankleR, ankleL))
                return true;

            return false;
        }
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/

        // FASE1
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/
        // Comprueba si esta avanzando en la hacia la posicion final de la Fase1 del ejercicio.
        // Mano hacia delante hasta quedar en paralelo.
        public bool TransMovimiento1(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL, float error)
        {
            if (elbowR.Position.Z < shoulderR.Position.Z - 0.07 + (0.07 * error) && wristR.Position.Z < shoulderR.Position.Z - 0.07 + (0.07 * error))
            {
                if (elbowL.Position.Z < shoulderL.Position.Z - 0.07 + (0.07 * error) && wristL.Position.Z < shoulderL.Position.Z - 0.07 + (0.07 * error))
                {
                    return true;
                }
            }
            return false;
        }

        // Comprueba si ha llegado a la posicion final de la Fase1.
        // Brazos en paralelo delante del cuerpo y en horizontal al suelo.
        public bool Fase1Final(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL, float error)
        {
            if (elbowR.Position.Z < shoulderR.Position.Z - 0.20 + (0.20 * error) && wristR.Position.Z < shoulderR.Position.Z - 0.20 + (0.20 * error))
            {
                if (elbowL.Position.Z < shoulderL.Position.Z - 0.20 + (0.20 * error) && wristL.Position.Z < shoulderL.Position.Z - 0.20 + (0.20 * error))
                {
                    return true;
                }
            }
            return false;
        }
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/

        // FASE2
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/
        // Comprueba si esta avanzando en la hacia la posicion final de la Fase2 del ejercicio.
        // Bajar poco a poco flexionando las rodillas y con la espalda recta.
        public bool TransMovimiento2(Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL, float error)
        {

            if ((hipC.Position.Y - kneeR.Position.Y) < 0.70 + (0.70 * error) && (hipC.Position.Y - kneeL.Position.Y) < 0.70 + (0.70 * error) &&
                (hipC.Position.Y - kneeR.Position.Y) > 0.53 + (0.53 * error) && (hipC.Position.Y - kneeL.Position.Y) > 0.53 + (0.53 * error))
            {
                return true;
            }//Esto o <
            return false;
        }

        // Comprueba si ha llegado a la posicion final de la Fase2.
        // Brazos en paralelo delante del cuerpo y en horizontal al suelo en sentadilla.
        public bool Fase2Final(Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL, float error)
        {
            if ((hipC.Position.Y - kneeR.Position.Y) < 0.52 + (0.52 * error) && (hipC.Position.Y - kneeL.Position.Y) < 0.52 + (0.52 * error))
            {
                return true;
            }
            return false;
        }
        
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/

        // ERROR
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/
        // Comprobacion de los casos de error.
        // Reinicial el ejercicio si se cumple cualquiera de ellos.
        public bool CasoError(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL, Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL, Joint ankleIR, Joint ankleIL, float error)
        {
            if (elbowR.Position.Y > shoulderR.Position.Y + 0.06 + (0.06 * error) || elbowR.Position.Y < shoulderR.Position.Y - 0.06 + (0.06 * error) ||
                wristR.Position.Y > shoulderR.Position.Y + 0.08 + (0.08 * error) || wristR.Position.Y < shoulderR.Position.Y - 0.08 + (0.08 * error) ||
                elbowL.Position.Y > shoulderL.Position.Y + 0.06 + (0.06 * error) || elbowL.Position.Y < shoulderL.Position.Y - 0.06 + (0.06 * error) ||
                wristL.Position.Y > shoulderL.Position.Y + 0.08 + (0.08 * error) || wristL.Position.Y < shoulderL.Position.Y - 0.08 + (0.08 * error) ||
                ankleR.Position.Y > ankleIR.Position.Y + 0.06 + (0.06 * error) || ankleL.Position.Y > ankleIL.Position.Y + 0.06 + (0.06 * error))
            {
                return true;
            }

            return false;
        }
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/

        // DETECCION DE POSTURAS
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/
        public bool PostureDetector(Posture posture)
        {
            if (postureStart != posture)
            {
                accumulator = 0;
                postureStart = posture;
                return false;
            }
            if (accumulator < PostureDetectionNumber)
            {
                accumulator++;
                return false;
            }
            if (posture != postureInicial)
            {
                accumulator = 0;
                postureInicial = posture;
                return true;
            }
            else
                accumulator = 0;
            return false;
        }
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/

        // FUNCION DE DETECCION DEL MOVIMIENTO
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/
        public void comprobarGestosBajada(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL,
                                    Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL, Joint ankleIR, Joint ankleIL, float error)
        {
            //solucionP.Content = "Hombro Y: " + shoulderR.Position.Z + Environment.NewLine + "Mano Y:" + wristR.Position.Z + Environment.NewLine + "Codo Y:" + elbowR.Position.Z;
            if (PosInicio(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, error))
            {
                if (PostureDetector(Posture.Inicio))
                {
                    solucionP.Content = "Postura de inicio de la bajada correcta." + Environment.NewLine + "Comience a realizar el primer ejercicio.";
                    reposo = true;
                    proceso1 = false;
                    proceso2 = false;
                    fin1 = false;
                    fin2 = false;
                    contError1 = 0;
                    contError2 = 0;
                }
            }
            else
            {
                // La primera postura que debe reconocer sera la de reposo sino
                // no dara comienzo el ejercicio.
                if (reposo)
                {
                    if (Fase1Final(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                    {
                        if (Fase2Final(hipC, kneeR, kneeL, ankleR, ankleL, error))
                        {
                            if (PostureDetector(Posture.Fase1))
                            {
                                proceso1 = false;
                                proceso2 = false;
                                fin1 = true;
                                fin2 = true;
                                contError1 = 0;
                                contError2 = 0;
                                numMov--;
                                if (numMov == 0)
                                {
                                    solucionP.Content = "Ha completado con exito el numero de repeticiones establecido." + Environment.NewLine + "Puede relajarse y descansar un poco.";
                                    reposo = true;
                                }
                                else
                                {
                                    solucionP.Content = "Ha realizado correctamente el primer ejercicio." + Environment.NewLine + "Comience a realizar el ejercicio inverso.";
                                    reposo = false;
                                    subida = true;
                                }
                            }
                        }
                    }
                    else if (Fase2Final(hipC, kneeR, kneeL, ankleR, ankleL, error))
                    {
                        if (Fase1Final(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                        {
                            if (PostureDetector(Posture.Fase1))
                            {
                                reposo = false;
                                proceso1 = false;
                                proceso2 = false;
                                fin1 = true;
                                fin2 = true;
                                contError1 = 0;
                                contError2 = 0;
                                numMov--;
                                if (numMov == 0)
                                    solucionP.Content = "Ha completado con exito el numero de repeticiones establecido." + Environment.NewLine + "Puede relajarse y descansar un poco.";
                                else
                                {
                                    solucionP.Content = "Ha realizado correctamente el primer ejercicio." + Environment.NewLine + "Comience a realizar el ejercicio inverso.";
                                    subida = true;
                                }
                            }
                        }
                    }
                    else if (PosInicio(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, error))
                    {
                        if (PostureDetector(Posture.Inicio))
                        {
                            reposo = true;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            solucionP.Content = "Postura de inicio de la bajada correcta." + Environment.NewLine + "Comience a realizar el primer ejercicio.";
                        }
                    }
                    else if (TransMovimiento1(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                    {
 
                        if (TransMovimiento2(hipC, kneeR, kneeL, ankleR, ankleL, error) && contError1 < 30)
                        {
                            if (PostureDetector(Posture.Transcurso1))
                            {
                                reposo = true;
                                proceso1 = true;
                                proceso2 = true;
                                fin1 = false;
                                fin2 = false;
                                contError1 = 0;
                                contError2 = 0;
                                solucionP.Content = "Mueva los brazos hacia delante hasta quedar en paralelo" + Environment.NewLine + " a la vez que realiza una sentadilla.";
                            }
                        }
                        else if(contError1 >= 50)
                        {
                            reposo = false;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            subida = false;
                            solucionP.Content = "Establezca la posicion inicial de la bajada y" + Environment.NewLine + "no realice movimientos raros1";
                        }
                        contError1++;
                    }
                    else if (TransMovimiento2(hipC, kneeR, kneeL, ankleR, ankleL, error))
                    {
                        if (TransMovimiento1(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error) && contError2 < 30)
                        {
                            if (PostureDetector(Posture.Transcurso1))
                            {
                                reposo = true;
                                proceso1 = true;
                                proceso2 = true;
                                fin1 = false;
                                fin2 = false;
                                contError2 = 0;
                                contError1 = 0;
                                solucionP.Content = "Mueva los brazos hacia delante hasta quedar en paralelo" + Environment.NewLine + " a la vez que realiza una sentadilla.";
                            }
                        }
                        else if (contError2 >= 50)
                        {
                            reposo = false;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError2 = 0;
                            contError1 = 0;
                            subida = false;
                            solucionP.Content = "Establezca la posicion inicial de la bajada y" + Environment.NewLine + "no realice movimientos raros2";
                        }
                        contError2++;
                    }
                    else if (CasoError(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, ankleIR, ankleIL, error))
                    {
                        if (PostureDetector(Posture.None))
                        {
                            reposo = false;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            subida = false;
                            solucionP.Content = "Establezca la posicion inicial de la bajada y" + Environment.NewLine + "no realice movimientos raros3";
                        }
                    }                   
                }
            }
        }
        public void comprobarGestosSubida(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL,
                                    Joint hipC, Joint kneeR, Joint kneeL, Joint ankleR, Joint ankleL, Joint ankleIR, Joint ankleIL, float error)
        {
            if (Fase1Final(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
            {
                if (Fase2Final(hipC, kneeR, kneeL, ankleR, ankleL, error))
                {
                    if (PostureDetector(Posture.Fase1))
                    {
                        reposo = false;
                        proceso1 = false;
                        proceso2 = false;
                        fin1 = true;
                        fin2 = true;
                        contError1 = 0;
                        contError2 = 0;
                        solucionP.Content = "Postura de inicio de la subida correcta." + Environment.NewLine + "Comience a realizar el segundo ejercicio.";
                    }
                }
            }
            else if (Fase2Final(hipC, kneeR, kneeL, ankleR, ankleL, error))
            {
                if (Fase1Final(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                {
                    if (PostureDetector(Posture.Fase1))
                    {
                        reposo = false;
                        proceso1 = false;
                        proceso2 = false;
                        fin1 = true;
                        fin2 = true;
                        contError1 = 0;
                        contError2 = 0;
                        solucionP.Content = "Postura de inicio de la subida correcta." + Environment.NewLine + "Comience a realizar el segundo ejercicio.";
                    }
                }
            }
            else
            {
                // La primera postura que debe reconocer sera la de reposo sino
                // no dara comienzo el ejercicio.
                if (fin1 && fin2)
                {
                    if (PosInicio(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, error))
                    {
                        if (PostureDetector(Posture.Inicio))
                        {
                            reposo = true;
                            proceso1 = false;
                            proceso2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            numMov--;
                            if (numMov <= 0)
                            {
                                solucionP.Content = "Ha completado con exito el numero de repeticiones establecido." + Environment.NewLine + "Puede relajarse y descansar un poco.";
                                fin1 = true;
                                fin2 = true;
                            }
                            else
                            {
                                solucionP.Content = "Ha realizado correctamente el segundo ejercicio." + Environment.NewLine + "Comience de nuevo.";
                                fin1 = false;
                                fin2 = false;
                                subida = false;
                            }
                        }
                    }
                    else if (Fase1Final(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                    {
                        if (Fase2Final(hipC, kneeR, kneeL, ankleR, ankleL, error))
                        {
                            if (PostureDetector(Posture.Fase1))
                            {
                                reposo = false;
                                proceso1 = false;
                                proceso2 = false;
                                fin1 = true;
                                fin2 = true;
                                contError1 = 0;
                                contError2 = 0;
                                solucionP.Content = "Postura de inicio de la subida correcta." + Environment.NewLine + "Comience a realizar el segundo ejercicio.";
                            }
                        }
                    }
                    else if (Fase2Final(hipC, kneeR, kneeL, ankleR, ankleL, error))
                    {
                        if (Fase1Final(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                        {
                            if (PostureDetector(Posture.Fase1))
                            {
                                reposo = false;
                                proceso1 = false;
                                proceso2 = false;
                                fin1 = true;
                                fin2 = true;
                                contError1 = 0;
                                contError2 = 0;
                                solucionP.Content = "Postura de inicio de la subida correcta." + Environment.NewLine + "Comience a realizar el segundo ejercicio.";
                            }
                        }
                    }
                    else if (TransMovimiento1(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error))
                    {

                        if (TransMovimiento2(hipC, kneeR, kneeL, ankleR, ankleL, error) && contError1 < 30)
                        {
                            if (PostureDetector(Posture.Transcurso1))
                            {
                                reposo = false;
                                proceso1 = true;
                                proceso2 = true;
                                fin1 = true;
                                fin2 = true;
                                contError1 = 0;
                                contError2 = 0;
                                solucionP.Content = "Mueva los brazos hacia atras hasta quedar en cruz" + Environment.NewLine + " a la vez que comienza a estirar las piernas.";
                            }
                        }
                        else if (contError1 >= 50)
                        {
                            reposo = false;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            subida = false;
                            numMov++;
                            solucionP.Content = "Establezca la posicion inicial de la bajada y" + Environment.NewLine + "no realice movimientos raros4";
                        }
                        contError1++;
                    }
                    else if (TransMovimiento2(hipC, kneeR, kneeL, ankleR, ankleL, error))
                    {
                        if (TransMovimiento1(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, error) && contError2 < 30)
                        {
                            if (PostureDetector(Posture.Transcurso1))
                            {
                                reposo = false;
                                proceso1 = true;
                                proceso2 = true;
                                fin1 = true;
                                fin2 = true;
                                contError1 = 0;
                                contError2 = 0;
                                solucionP.Content = "Mueva los brazos hacia atras hasta quedar en cruz" + Environment.NewLine + " a la vez que comienza a estirar las piernas.";
                            }
                        }
                        else if (contError2 >= 50)
                        {
                            reposo = false;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            subida = false;
                            numMov++;
                            solucionP.Content = "Establezca la posicion inicial de la bajada y" + Environment.NewLine + "no realice movimientos raros5";
                        }
                        contError2++;
                    }
                    else if (CasoError(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL, hipC, kneeR, kneeL, ankleR, ankleL, ankleIR, ankleIL, error))
                    {
                        if (PostureDetector(Posture.None))
                        {
                            reposo = false;
                            proceso1 = false;
                            proceso2 = false;
                            fin1 = false;
                            fin2 = false;
                            contError1 = 0;
                            contError2 = 0;
                            subida = false;
                            numMov++;
                            solucionP.Content = "Establezca la posicion inicial de la bajada y" + Environment.NewLine + "no realice movimientos raros6";
                        }
                    }
                }
            }
        }
        /***************************************************************************************************************************************************************/
        /***************************************************************************************************************************************************************/

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                // Pintamos el hueso Hombro - Codo segun la posicion en la que se encuentra
                if (jointType0 == JointType.HipCenter && jointType1 == JointType.HipLeft || jointType0 == JointType.HipCenter && jointType1 == JointType.HipRight)
                {
                    drawPen = selectColorPiernas();
                }
                else if (jointType0 == JointType.HipLeft && jointType1 == JointType.KneeLeft || jointType0 == JointType.HipRight && jointType1 == JointType.KneeRight)
                {
                    drawPen = selectColorPiernas();
                }
                else if (jointType0 == JointType.KneeLeft && jointType1 == JointType.AnkleLeft || jointType0 == JointType.KneeRight && jointType1 == JointType.AnkleRight)
                {
                    drawPen = selectColorPiernas();
                }
                else if (jointType0 == JointType.AnkleLeft && jointType1 == JointType.FootLeft || jointType0 == JointType.AnkleRight && jointType1 == JointType.FootRight)
                {
                    drawPen = selectColorPiernas();
                }
                else if (jointType0 == JointType.ShoulderRight && jointType1 == JointType.ElbowRight || jointType0 == JointType.ShoulderLeft && jointType1 == JointType.ElbowLeft)
                {
                    drawPen = selectColorBrazos();
                }
                else if (jointType0 == JointType.ElbowRight && jointType1 == JointType.WristRight || jointType0 == JointType.ElbowLeft && jointType1 == JointType.WristLeft)
                {
                    drawPen = selectColorBrazos();
                }
                else if (jointType0 == JointType.WristRight && jointType1 == JointType.HandRight || jointType0 == JointType.WristLeft && jointType1 == JointType.HandLeft)
                {
                    drawPen = selectColorBrazos();
                }
                else
                    drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Seleccion el pen del color con el que pintar los brazos
        /// </summary>
        public Pen selectColorBrazos()
        {
            if (!subida)
            {
                if (reposo)
                {
                    if (proceso1)
                        return penProceso;
                    else if (fin1)
                        return penFin;
                    else
                        return penInicio;
                }
                else
                    return penError;
            }
            else
            {
                if (fin1)
                {
                    if (proceso1)
                        return penProceso;
                    else if (reposo)
                        return penFin;
                    else
                        return penInicio;
                }
                else
                    return penError;
            }
        }

        /// <summary>
        /// Seleccion el pen del color con el que pintar las piernas
        /// </summary>
        public Pen selectColorPiernas()
        {
            if (!subida)
            {
                if (reposo)
                {
                    if (proceso2)
                        return penProceso;
                    else if (fin2)
                        return penFin;
                    else
                        return penInicio;
                }
                else
                    return penError;
            }
            else
            {
                if (fin2)
                {
                    if (proceso2)
                        return penProceso;
                    else if (reposo)
                        return penFin;
                    else
                        return penInicio;
                }
                else
                    return penError;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            reposo = false;
            proceso1 = false;
            proceso2 = false;
            fin1 = false;
            fin2 = false;
            contError1 = 0;
            contError2 = 0;
            subida = false;
            numMov = 1;
            solucionP.Content = "Establezca la posicion inicial";
            repeticiones.Text = "1";
            movRestantes.Text = numMov + "";
            
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int aux = (int)this.slider1.Value;
            error = (float)aux / 100;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            numMov = Convert.ToInt32(repeticiones.Text);
            movRestantes.Text = numMov + "";
        }
    }
}