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

    public enum Posture
    {
        None,
        Inicio,
        Correcto,
        Transcurso,
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PostureDetectionNumber = 10;
        int accumulator = 0;
        Posture postureInitial = Posture.None;
        Posture postureAtras = Posture.Transcurso;
        Posture postureFinal = Posture.Correcto;
        Posture postureStart = Posture.Inicio;

        // Puntos de union y Pens con los que vamos a pintar los huesos del cuerpo segun el movimiento
        Joint wristR, elbowR, shoulderR, wristL, elbowL, shoulderL;
        private readonly Pen penCorrecto = new Pen(Brushes.Green, 6);
        private readonly Pen penTranscurso = new Pen(Brushes.Yellow, 6);
        private readonly Pen penInicio = new Pen(Brushes.Blue, 6);
        private readonly Pen penError = new Pen(Brushes.Red, 6);

        // Booleanos para controlar la posicion y pintar los huesos de distinto color
        private bool reposo = false;
        private bool proceso = false;
        private bool correcto = false;

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

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
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
                    wristR = bones.Joints[JointType.WristRight]; //MUÑECA
                    elbowR = bones.Joints[JointType.ElbowRight]; //CODO
                    shoulderR = bones.Joints[JointType.ShoulderRight]; //HOMBRO
                    wristL = bones.Joints[JointType.WristLeft]; //MUÑECA
                    elbowL = bones.Joints[JointType.ElbowLeft]; //CODO
                    shoulderL = bones.Joints[JointType.ShoulderLeft]; //HOMBRO
                }
            }

            // Llamada a las comprobaciones de la posicion del brazo, 
            // para que acceda el punto del hombro debe estar en tracking
            // sino produce errores.
            if (shoulderR.TrackingState == JointTrackingState.Tracked)
                comprobarGestos(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL);           
        }

        // Comprueba si se encuentra en la posicion inicial
        // Mano derecha en cruz.
        public bool PosInicio(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL)
        {
            if (elbowR.Position.Y < shoulderR.Position.Y + 0.03 && elbowR.Position.Y > shoulderR.Position.Y - 0.03 &&
                wristR.Position.Y < shoulderR.Position.Y + 0.05 && wristR.Position.Y > shoulderR.Position.Y - 0.05
                && elbowR.Position.Z < shoulderR.Position.Z + 0.03 && elbowR.Position.Z > shoulderR.Position.Z - 0.1 &&
                wristR.Position.Z < shoulderR.Position.Z + 0.03 && wristR.Position.Z > shoulderR.Position.Z - 0.15)
            {
                if (elbowL.Position.Y < shoulderL.Position.Y + 0.03 && elbowL.Position.Y > shoulderL.Position.Y - 0.03 &&
                wristL.Position.Y < shoulderL.Position.Y + 0.05 && wristL.Position.Y > shoulderL.Position.Y - 0.05
                && elbowL.Position.Z < shoulderL.Position.Z + 0.03 && elbowL.Position.Z > shoulderL.Position.Z - 0.1 &&
                wristL.Position.Z < shoulderL.Position.Z + 0.03 && wristL.Position.Z > shoulderL.Position.Z - 0.15)
                {
                    return true;
                }
            }
            return false;
        }

        // Comprueba si esta avanzando en la hacia la posicion final.
        // Mano en cruz retrasada respecto al hombro.
        public bool TransMovimiento(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL)
        {
            if (elbowR.Position.Z < shoulderR.Position.Z - 0.04 && wristR.Position.Z < shoulderR.Position.Z - 0.04)
            {
                if (elbowL.Position.Z < shoulderL.Position.Z - 0.04 && wristL.Position.Z < shoulderL.Position.Z - 0.04)
                {
                    return true;
                }
            }
            return false;
        }

        // Comprueba si ha llegado a la posicion final del movimiento.
        // Mano en cruz atrasada respecto al hombro hasta el limite.
        public bool MovFinalizado(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL)
        {
            if (elbowR.Position.Z < shoulderR.Position.Z - 0.22 && wristR.Position.Z < shoulderR.Position.Z - 0.22)
            {
                if (elbowL.Position.Z < shoulderL.Position.Z - 0.22 && wristL.Position.Z < shoulderL.Position.Z - 0.22)
                {
                    return true;
                }
            }
            return false;
        }

        // Comprobacion de los casos de error.
        // Reinicial el ejercicio si se cumple cualquiera de ellos.
        public bool CasoError(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL)
        {
            if (elbowR.Position.Z > shoulderR.Position.Z - 0.03 || wristR.Position.Z > shoulderR.Position.Z - 0.03 || 
                elbowR.Position.Y > shoulderR.Position.Y + 0.03 || elbowR.Position.Y < shoulderR.Position.Y - 0.03 ||
                wristR.Position.Y > shoulderR.Position.Y + 0.05 || wristR.Position.Y < shoulderR.Position.Y - 0.05 ||
                elbowL.Position.Z > shoulderL.Position.Z - 0.03 || wristL.Position.Z > shoulderL.Position.Z - 0.03 ||
                elbowL.Position.Y > shoulderL.Position.Y + 0.03 || elbowL.Position.Y < shoulderL.Position.Y - 0.03 ||
                wristL.Position.Y > shoulderL.Position.Y + 0.05 || wristL.Position.Y < shoulderL.Position.Y - 0.05)
            {
                return true;
            }
            return false;
        }

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
            if (posture != postureInitial)
            {
                accumulator = 0;
                postureInitial = posture;
                return true;
            }
            else
                accumulator = 0;
            return false;
        }

        // Metodo general de comprobacion de gestos.
        public void comprobarGestos(Joint wristR, Joint elbowR, Joint shoulderR, Joint wristL, Joint elbowL, Joint shoulderL)
        {
            if (PosInicio(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL))
            {
                if (PostureDetector(Posture.Inicio))
                {
                    solucionP.Content = "Postura de inicio correcta";
                    reposo = true;
                    correcto = false;
                    proceso = false;
                }
            }
            else
            {
                // La primera postura que debe reconocer sera la de reposo sino
                // no dara comienzo el ejercicio.
                if (reposo)
                {
                    if (MovFinalizado(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL))
                    {
                        if (PostureDetector(Posture.Correcto))
                        {
                            correcto = true;
                            proceso = false;
                            solucionP.Content = "Moviento finalizado correctamente";
                        }
                    }
                    else if (PosInicio(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL))
                    {
                        if (PostureDetector(Posture.Inicio))
                        {
                            correcto = false;
                            proceso = false;
                            solucionP.Content = "Postura de inicio, comienze";
                        }
                    }
                    else if (CasoError(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL))
                    {
                        if (PostureDetector(Posture.None))
                        {
                            correcto = false;
                            proceso = false;
                            reposo = false;
                            solucionP.Content = "Establezca la posicion inicial";
                        }
                    }
                    else if (TransMovimiento(wristR, elbowR, shoulderR, wristL, elbowL, shoulderL))
                    {
                        if (PostureDetector(Posture.Transcurso))
                        {
                            proceso = true;
                            correcto = false;
                            solucionP.Content = "Mueva la mano hacia atras";
                        }
                    }
                    
                }

                // Si salta un caso de error o no se establece la pos de reposo
                // al iniciar el ejercicio.
                else if (PostureDetector(Posture.None))
                {
                    if (PostureDetector(Posture.None))
                    {
                        correcto = false;
                        proceso = false;
                        reposo = false;
                        solucionP.Content = "Establezca la posicion inicial";
                    }
                }
            }
        }

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
                if (jointType0 == JointType.ShoulderRight && jointType1 == JointType.ElbowRight || jointType0 == JointType.ShoulderLeft && jointType1 == JointType.ElbowLeft)
                {
                    drawPen = selectColor();
                }
                // Pintamos el hueso Codo - Muñeca segun la posicion en la que se encuentra
                else if (jointType0 == JointType.ElbowRight && jointType1 == JointType.WristRight || jointType0 == JointType.ElbowLeft && jointType1 == JointType.WristLeft)
                {
                    drawPen = selectColor();
                }
                // Pintamos el hueso Muñeca - Mano segun la posicion en la que se encuentra
                else if (jointType0 == JointType.WristRight && jointType1 == JointType.HandRight || jointType0 == JointType.WristLeft && jointType1 == JointType.HandLeft)
                {
                    drawPen = selectColor();
                }
                // Pintamos el resto de huesos con el color por defecto
                else
                    drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        // Metodo para la seleccion del color segun la posicion
        // en la que se encuentre el brazo.
        public Pen selectColor()
        {
            if (reposo)
            {
                if (proceso)
                    return penTranscurso;
                else if (correcto)
                    return penCorrecto;
                else
                    return penInicio;
            }
            else
                return penError;
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }
    }
}