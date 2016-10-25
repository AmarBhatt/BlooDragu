using MyoSharp.Communication;
using MyoSharp.Device;
using MyoSharp.Exceptions;
using MyoSharp.Math;
using MyoSharp.Poses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public class Matrix3F
    {
        public float[] values = new float[9];

        public Matrix3F()
        {

            for (uint i = 0; i < 3; ++i)
            {
                for (uint j = 0; j < 3; ++j)
                {
                    if (i == j)
                        values[i * 3 + j] = 1;
                    else
                        values[i * 3 + j] = 0;
                }
            }
        }

        public static Vector3F operator*(Matrix3F mat, Vector3F vec)
        {
            float[] result = new float[3];

            for(uint i =0;i<3;++i)
            {
                result[i] = 0;
                for(uint j =0;j<3;++j)
                {
                    result[i] += mat.values[i * 3 + j] * vec[j];
                }
            }
            return new Vector3F(result[0],result[1], result[2]);
        }

        public static Matrix3F Create(Vector3F vec)
        {
            var unit_vec = vec / vec.Magnitude();

            var mat = new Matrix3F();
            mat.values[0] = unit_vec[0];
            mat.values[1] = unit_vec[1];
            mat.values[2] = unit_vec[2];

            var cosy = Math.Atan(unit_vec[1] / unit_vec[0]);


            mat.values[3] = unit_vec[0];

            return mat;
        }
    }
    public class MyoControl : IDisposable
    {
        IChannel m_channel;
        IHub m_hub;
        IMyo m_myo;


        protected MyoControl()
        {
        }

        static MyoControl m_instance;
        public static MyoControl GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = new MyoControl();
                m_instance.m_channel = Channel.Create(
                    ChannelDriver.Create(ChannelBridge.Create(),
                    MyoErrorHandlerDriver.Create(MyoErrorHandlerBridge.Create())));

                m_instance.m_hub = Hub.Create(m_instance.m_channel);

                m_instance.m_hub.MyoConnected += m_hub_MyoConnected;

                m_instance.m_channel.StartListening();
            }

            return m_instance;
        }

        public event EventHandler OnPoseChanged;
        private void M_myo_PoseChanged(object sender, PoseEventArgs e)
        {
            if (OnPoseChanged != null)
            {
                OnPoseChanged(this, null);
            }
        }

        public bool IsConnected { get { return m_myo != null; } }

        public Vector3F Gyroscope
        {
            get
            {
                if (m_myo != null)
                {
                    return m_myo.Gyroscope;
                }
                return null;
            }
        }

        public Vector3F Accelerometer
        {
            get
            {
                if (m_myo != null)
                {
                    return m_myo.Accelerometer;
                }
                return null;
            }
        }

        public QuaternionF Orientation
        {
            get
            {
                if (m_myo != null)
                {
                    return m_myo.Orientation;
                }
                return null;
            }
        }

        public int? Gesture
        {
            get
            {
                if (m_myo != null)
                {
                    switch (m_myo.Pose)
                    {
                        case Pose.FingersSpread:
                            return 1;
                        case Pose.Fist:
                            return 2;
                        case Pose.WaveIn:
                            return 3; // Elbow
                        case Pose.WaveOut:
                            return 5; // Base
                        case Pose.DoubleTap:
                            return 4;
                        case Pose.Rest:
                        case Pose.Unknown:
                        default:
                            return 0;
                    }
                }
                return null;
            }
        }

        static void m_hub_MyoConnected(object sender, MyoEventArgs e)
        {
            if (m_instance.m_myo == null)
            {
                m_instance.m_myo = e.Myo;
                m_instance.m_myo.PoseChanged += m_instance.M_myo_PoseChanged;
                m_instance.m_myo.Unlock(UnlockType.Hold);
                m_instance.m_myo.Disconnected += (s, e2) =>
                {
                    m_instance.m_myo = null;
                    Debug.WriteLine("Myo Armband disconnected");
                };
            }
            else
            {
                Debug.WriteLine("Cannot connect to multiple Myo Armbands!");
            }
        }

        public void Dispose()
        {
            m_hub.Dispose();
            m_channel.Dispose();
        }
    }
}
