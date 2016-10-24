using MyoSharp.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public class ArmState
    {
        public float[] Angles = new float[6];

        public ArmState()
        {
            for (var i = 0; i < Angles.Length; ++i)
                Angles[i] = 7;
        }

        public const float MAX_DUTY = 12;
        public const float MIN_DUTY = 5;
        public const float CLAW_OPEN = 5;
        public const float CLAW_CLOSED = 12;
        public const int CLAW = 0;

        public void Update(int idx, float step)
        {
            Angles[idx] += step;
            Angles[idx] = Math.Max(Math.Min(Angles[idx], MAX_DUTY), MIN_DUTY);
        }

        public void SetAngle(int idx, float duty)
        {
            Angles[idx] = Math.Max(Math.Min(duty, MAX_DUTY), MIN_DUTY);
        }

        public void ToggleClaw(bool open)
        {
            if (open)
            {
                Angles[CLAW] = CLAW_OPEN;
            }
            else
            {
                Angles[CLAW] = CLAW_CLOSED;
            }
            
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
            else
            {
                Console.WriteLine("Disconnect: {0}", str);
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
