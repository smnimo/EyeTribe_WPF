﻿using System;
//using System.Linq;
//using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
//using System.Windows.Input;
using System.Windows.Media;
//using System.Windows.Media.Imaging;
using TETCSharpClient;
using TETCSharpClient.Data;
using MessageBox = System.Windows.MessageBox;
using System.IO;
using System.Collections;

namespace eyetracker
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : IGazeListener
    {
        #region Variables
        private Matrix transfrm;
        private StreamWriter swt;
        private System.Drawing.Size monitorSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
        #endregion

        #region Constructor
        public MainWindow()
        {
            // file I/O
            string now = DateTime.Now.ToString("yyMMddHHmmss");
            string csvname = String.Format("./{0}.csv", now);
            swt = new StreamWriter(csvname);

            // eye tribe
            GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);
            if (!GazeManager.Instance.IsActivated)
            {
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("EyeTribe Server has not been started")));
                Close();
                return;
            }
            // Register for key events
            KeyDown += WindowKeyDown;

            InitializeComponent();
        }
        #endregion

        #region Public methods
        public void OnGazeUpdate(GazeData gazeData)
        {
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            if (x == 0 & y == 0) return;
            // Invoke thread
            Dispatcher.BeginInvoke(new Action(() => UpdateUI(x, y)));
        }
        #endregion

        #region privatemethods
        private void UpdateUI(int x, int y)
        {
            // Unhide the GazePointer if you want to see your gaze point
            if (GazePointer.Visibility == Visibility.Visible)
            {
                var relativePt = new Point(x, y);
                relativePt = transfrm.Transform(relativePt);
                Canvas.SetLeft(GazePointer, relativePt.X - GazePointer.Width / 2);
                Canvas.SetTop(GazePointer, relativePt.Y - GazePointer.Height / 2);
            }
            swt.Write("{0}, {1}, {2}\n", DateTime.Now.ToString("yyMMddHHmmssffff"), x, y);
        }

        private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Close();
        }
        private void CleanUp()
        {
            GazeManager.Instance.Deactivate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            CleanUp();
            swt.Close();
            base.OnClosing(e);
        }

        #endregion
    }
}
