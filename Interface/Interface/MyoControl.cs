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

        private static void M_myo_PoseChanged(object sender, PoseEventArgs e)
        {
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
                m_instance.m_myo.PoseChanged += M_myo_PoseChanged;
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
