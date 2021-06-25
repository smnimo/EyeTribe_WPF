using System;
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
        private double[,] roi;
        private int[] roi_hist;
        private System.Drawing.Size monitorSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
        #endregion

        #region Constructor
        public MainWindow()
        {
            // file I/O
            string now = DateTime.Now.ToString("yyMMddhhmmss");
            string csvname = String.Format("./{0}.csv", now);
            swt = new StreamWriter(csvname);

            // read annotated regions from data.csv
            string datapath = ("../../roi.csv");
            double[,] roi_org = readfromcsv(datapath); // id, x, y, width, height, roi[0, :]: imageSize
            roi = adjustroi(roi_org);

            double max_roi_id = 0;
            for (int j = 0; j < roi.GetLongLength(0); j++)
            {
                swt.WriteLine("RoI_id{0}: x:{1}--{2}, y:{3}--{4}",  roi[j,0], roi[j,1], roi[j,1] + roi[j,3], roi[j,2], roi[j,2] + roi[j,4]);
                max_roi_id = (max_roi_id > roi[j,0])? max_roi_id : roi[j,0]; //0: add empty region for y axis, 1: add empty region for x axis
            }
            roi_hist = new int[(int)max_roi_id+1];
            
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
            DoRoICheck(x, y);
        }

        private double[,] readfromcsv(string datapath)
        {
            // read from file
            StreamReader objReader = new StreamReader(datapath);
            string sLine = "";
            ArrayList arrText = new ArrayList();
            while (sLine != null)
            {
                sLine = objReader.ReadLine();
                if (sLine != null)
                {
                    arrText.Add(sLine);
                }
            }
            objReader.Close();

            // count
            int line_count = arrText.Count;
            string tmp = (string)arrText[0];
            string[] tmp2 = tmp.Split(',');
            int col_count = tmp2.Length;

            // ArrayList --> arry
            double[,] InData = new double[line_count, col_count];
            int a = 0, b = 0;
            foreach (string sOutput in arrText)
            {
                string[] tmp_line = sOutput.Split(',');
                foreach (string value in tmp_line)
                {
                    InData[a, b] = Convert.ToDouble(value);
                    b++;
                }
                b = 0;
                a++;
            }
            return InData;
        }
        private double[,] adjustroi(double[,] roi_org)
        {
            double exRatio = (monitorSize.Width / roi_org[0, 0] < monitorSize.Height / roi_org[0, 1]) ? monitorSize.Width / roi_org[0, 0] : monitorSize.Height / roi_org[0, 1];
            int exdim = (monitorSize.Width / roi_org[0, 0] < monitorSize.Height / roi_org[0, 1]) ? 0 : 1; //0: add empty region for y axis, 1: add empty region for x axis
            double[,] roi_out = new double[roi_org.GetLongLength(0)-1, roi_org.GetLongLength(1)];
            for (int j = 0; j < roi_out.GetLongLength(0); j++)
            {
                roi_out[j, 0] = roi_org[j + 1, 0];
                for (int i = 1; i < roi_out.GetLongLength(1); i++)
                {
                    roi_out[j, i] = roi_org[j + 1, i] * exRatio;
                }
            }
            // only expand, ignore empty region

            if (exdim == 0)
            {
                int addspace = (int)(monitorSize.Height - roi_org[0, 1] * exRatio) / 2;
                for (int j = 0; j < roi_out.GetLongLength(0); j++)
                {
                    roi_out[j, 2] += addspace;
                }
                swt.WriteLine("ImageRegion: x:{0}--{1}, y:{2}--{3}", 0, roi_org[0,0]*exRatio, addspace, addspace + roi_org[0, 1]*exRatio);
            }
            else
            {
                int addspace = (int)(monitorSize.Width - roi_org[0, 0] * exRatio) / 2;
                for(int j = 0; j < roi_out.GetLongLength(0); j++)
                {
                    roi_out[j, 1] += addspace;
                }
                swt.WriteLine("ImageRegion: x:{0}--{1}, y:{2}--{3}", addspace, addspace + roi_org[0, 0] * exRatio, 0, roi_org[0, 1] * exRatio);
            }
            return roi_out;
        }
        private void DoRoICheck(int x, int y)
        {
            var gazePt = new Point(x, y);
            gazePt = transfrm.Transform(gazePt);
            int roi_id = -1;
            for (int j = 0; j < roi.GetLongLength(0); j++ )
            {
                if(gazePt.X > roi[j,1] && gazePt.X < (roi[j,1] + roi[j,3]) && gazePt.Y > roi[j,2] && gazePt.Y < (roi[j,2] + roi[j,4]))
                {
                    roi_id = (int)roi[j, 0];
                    break;
                }
            }
            if (roi_id >= 0)
            {
                roi_hist[roi_id]++;
            }
            swt.WriteLine("{0}, {1}, {2}", x, y, roi_id);
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
            for (int i = 0; i < roi_hist.GetLength(0); i++)
            {
                swt.Write("{0}, ", roi_hist[i]);
            }
            swt.WriteLine("");
            swt.Close();
            base.OnClosing(e);
        }

        #endregion
    }
}
