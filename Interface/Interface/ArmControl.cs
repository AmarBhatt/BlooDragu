using MyoSharp.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public class ArmState : INotifyPropertyChanged
    {
        public const int NO_JOINTS = 6;
        private float[] Angles = new float[NO_JOINTS];
        private bool claw = true;

        protected ArmState() { }
        static ArmState a_instance;

        public static ArmState GetInstance()
        {
            if (a_instance == null)
            {
                a_instance = new ArmState();
                for (var i = 0; i < NO_JOINTS; ++i)
                {
                    a_instance[i] = reset_duty[i];
                    a_instance.OnLimit[i] = false;
                }

                a_instance[CLAW_IDX] = a_instance.CLAW_OPEN;
                a_instance.ClawState = true;
            }
            return a_instance;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ClawState
        {
            get { return claw; }
            set
            {
                claw = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("ClawState");
            }
        }

        public float this[int idx]
        {
            get { return Angles[idx]; }
            set
            {
                Angles[idx] = value;
                OnPropertyChanged(null);
            }
        }

        public float[] ANGLES
        {
            get { return Angles; }
        }

        public void Print()
        {
            Console.WriteLine(string.Join(" ", Angles));
        }

        public void Home()
        {
            for (var i = 0; i < NO_JOINTS; ++i)
            {
                a_instance[i] = reset_duty[i];
                a_instance.OnLimit[i] = false;
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public static readonly float[] reset_duty = { 12.0f, 8.0f, 7.0f, 7.0f, 7.0f, 7.0f};
        public float[] max_duty = { 12.0f, 12.0f , 12.0f , 12.0f , 8.0f , 8.0f};

        public float[] MAX_DUTY
        {
            get { return max_duty; }
            set
            {
                max_duty = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("MAX_DUTY");
            }
        }


        public float[] min_duty = { 9.0f, 5.0f, 5.0f, 7.0f, 5.0f, 3.0f };

        public float[] MIN_DUTY
        {
            get { return min_duty; }
            set
            {
                min_duty = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("MIN_DUTY");
            }
        }

        public const int CLAW_IDX = 0;
        public float CLAW_OPEN { get { return MAX_DUTY[CLAW_IDX]; } }
        public float CLAW_CLOSED { get { return MIN_DUTY[CLAW_IDX]; } }
        public void ToggleClaw()
        {
            if (a_instance[CLAW_IDX] > CLAW_CLOSED)
            {
                a_instance[CLAW_IDX] = CLAW_CLOSED;
                a_instance.ClawState = false;
            }
            else
            {
                a_instance[CLAW_IDX] = CLAW_OPEN;
                a_instance.ClawState = true;
            }

         }
        
        public event EventHandler OnLimitReached;

        public bool[] OnLimit = { false, false, false, false, false, false };
        public bool Update(int idx, float step)
        {
            var new_val = a_instance[idx] + step;
            if (new_val > MAX_DUTY[idx] || new_val < MIN_DUTY[idx])
            {
                a_instance[idx] = Math.Max(Math.Min(new_val, MAX_DUTY[idx]), MIN_DUTY[idx]);
                if(OnLimit[idx] == false)
                {
                    OnLimit[idx] = true;
                    OnLimitReached(this, null);
                }
                return false;
            }
            else
            {
                OnLimit[idx] = false;
                a_instance[idx] = new_val;
                return true;
            }
            //Console.WriteLine("Updating");
        }
        public void Set(int idx, float angle)
        {
            a_instance[idx] = Math.Max(Math.Min(angle, MAX_DUTY[idx]), MIN_DUTY[idx]);
        }
    }
    public class ArmControl
    {
        SerialPort m_serial = new SerialPort();

        protected ArmControl() { }
        static ArmControl m_instance;

        public bool IsConnected { get { return m_serial.IsOpen; } }

        public static ArmControl GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = new ArmControl();
            }
            return m_instance;
        }

        public bool Open(string port)
        {
            m_serial.PortName = port;

            try
            {
                m_serial.Open();
                m_serial.NewLine = "\r\n";
                m_serial.DataReceived += m_serial_DataReceived;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to open serial port ({0})!", ex.Message);
            }
            return m_serial.IsOpen;
        }

        public void SendPosition(ArmState state)
        {
            var str = string.Join(" ", state.ANGLES);
            
            if (m_serial.IsOpen)
            {
                lock (m_serial)
                {
                    m_serial.WriteLine(str);
                }
            }
        }

        private void m_serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (m_serial)
                if (m_serial.BytesToRead > 10)
                {
                    var length = m_serial.BytesToRead;
                    var buffer = new char[length + 1];
                    buffer[length] = (char)0;
                    m_serial.Read(buffer, 0, length);
                    //Console.Write(buffer);
                    // Read correct number of bits
                }
        }

        public static string[] ListPortNames()
        {
            return SerialPort.GetPortNames();
        }
    }
}
