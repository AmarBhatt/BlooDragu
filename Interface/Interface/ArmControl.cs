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
        public float[] Angles = new float[6];
        private bool claw = false;

        protected ArmState() { }
        static ArmState a_instance;

        public static ArmState GetInstance()
        {
            if (a_instance == null)
            {
                a_instance = new ArmState();
                for (var i = 0; i < a_instance.Angles.Length; ++i)
                    a_instance.Angles[i] = 7;

                a_instance.Angles[CLAW_IDX] = CLAW_CLOSED;
                a_instance.claw = true;
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
                Console.WriteLine("Test0");
                OnPropertyChanged("ClawState");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            Console.WriteLine("Test1");
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
                Console.WriteLine("Test2");
            }
        }


        //public ArmState()
        //{
        //    for (var i = 0; i < Angles.Length; ++i)
        //        Angles[i] = 7;

        //    Angles[CLAW_IDX] = CLAW_CLOSED;
        //}

        public float[] MAX_DUTY = { 11.0f, 12.0f , 12.0f , 12.0f , 8.0f , 12.0f};

        public const float MIN_DUTY = 5;

        public const int CLAW_IDX = 0;
        public const float CLAW_CLOSED = 8.2f;
        public const float CLAW_OPEN = 11.0f;
        public void ToggleClaw()
        {
            if (Angles[CLAW_IDX] > CLAW_CLOSED)
            {
                Angles[CLAW_IDX] = CLAW_CLOSED;
                a_instance.ClawState = false;
            }
            else
            {
                Angles[CLAW_IDX] = CLAW_OPEN;
                a_instance.ClawState = true;
            }

         }

        

        public void Update(int idx, float step)
        {
            Angles[idx] += step;
            Angles[idx] = Math.Max(Math.Min(Angles[idx], MAX_DUTY[idx]), MIN_DUTY);
        }
        public void Set(int idx, float angle)
        {
            Angles[idx] = Math.Max(Math.Min(angle, MAX_DUTY[idx]), MIN_DUTY);
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
            var str = string.Join(" ", state.Angles);
            if (m_serial.IsOpen)
            {
                lock (m_serial)
                    m_serial.WriteLine(str);
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
