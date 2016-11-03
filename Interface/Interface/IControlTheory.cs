using MyoSharp.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public abstract class IControlTheory : INotifyPropertyChanged
    {
        protected MyoControl m_myo;
        protected ArmControl m_arm;
        protected ArmState CurrentState;

        public IControlTheory()
        {
            CurrentState = ArmState.GetInstance();
        }

        static IControlTheory m_attached;

        public virtual void Attach(MyoControl MyoControl, ArmControl ArmControl)
        {
            if (m_attached != null)
            {
                m_attached.Detach();
            }
            m_myo = MyoControl;
            m_arm = ArmControl;
            m_attached = this;
            m_myo.OnPoseChanged += OnPoseChanged;
        }


        virtual protected void OnPoseChanged(object sender, EventArgs e)
        {
        }

        public virtual void Detach()
        {
            m_myo.OnPoseChanged -= OnPoseChanged;
            m_myo = null;
            m_arm = null;
            m_attached = null;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public abstract class PoolingControlTheory : IControlTheory
    {
        TimeSpan m_update_interval;
        TimeSpan m_loop_interval;
        bool m_running;
        Task m_arm_task;
        Task m_compute_task;
        public override void Attach(MyoControl MyoControl, ArmControl ArmControl)
        {
            base.Attach(MyoControl, ArmControl);
            m_running = true;
            m_update_interval = TimeSpan.FromMilliseconds(100);
            Setup();
            m_arm_task = Task.Run(async () =>
            {
                while (m_running)
                {
                    SendUpdate();
                    await Task.Delay(m_update_interval);
                }
            });
            m_compute_task = Task.Run(() =>
            {
                var time = DateTime.UtcNow;
                while (m_running)
                {
                    var span = DateTime.UtcNow.Subtract(time);
                    time = DateTime.UtcNow;
                    Loop(span);
                }
            });
        }

        virtual protected void Setup() { }
        abstract protected void Loop(TimeSpan span);

        virtual protected void SendUpdate()
        {
            m_arm.SendPosition(CurrentState);
            CurrentState.Print();
        }

        public override void Detach()
        {
            m_running = false;
            m_arm_task.Wait();
            m_compute_task.Wait();
            base.Detach();
        }
    }

    public class BetterControl : PoolingControlTheory
    {
        public BetterControl()
        {
            MaxVelocity = 0.00001f;
            AccelerationScale = 0.01f;
            StepSize = 0.0001f;
        }
        protected bool is_updating = false;
        public bool LockState
        {
            get { return is_updating; }
            set
            {
                is_updating = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("LockState");
            }
        }

        public TimeSpan claw_interval = TimeSpan.FromSeconds(1);
        public TimeSpan joint_interval = TimeSpan.FromSeconds(1);
        protected DateTime last_claw = DateTime.UtcNow;
        protected DateTime last_joint_change = DateTime.UtcNow;
        protected override void OnPoseChanged(object sender, EventArgs e)
        {
            base.OnPoseChanged(sender, e);

            if ((m_myo.Gesture == 4 || m_myo.Gesture == 2) && DateTime.UtcNow.Subtract(last_claw) > claw_interval)
            {
                if (m_myo.Gesture == 4)
                {
                    CurrentState.ToggleClaw();
                    Console.WriteLine("Claw toggled");
                }
                else
                {
                    LockState = !LockState;
                }
                last_claw = DateTime.UtcNow;
            }
            else if ((m_myo.Gesture == 3 || m_myo.Gesture == 5) && DateTime.UtcNow.Subtract(last_joint_change) > joint_interval && LockState)
            {
                if (m_myo.Gesture == 3)
                {
                    CurrentJoint--;
                    if (CurrentJoint < 1)
                        CurrentJoint = 5;
                    Console.WriteLine("switch to joint: {0}", CurrentJoint);
                }
                else
                {
                    CurrentJoint++;
                    if(CurrentJoint > 5)
                        CurrentJoint = 1;
                    Console.WriteLine("switch to joint: {0}", CurrentJoint);
                }
            }
        }
        protected override void Setup()
        {
            base.Setup();

            while (m_myo.IsConnected == false)
                Task.Delay(100).Wait();
        }

        public float MaxVelocity { get; set; }
        public float AccelerationScale { get; set; }
        public float StepSize { get; set; }

        
        protected int current_joint = 1;
        public int CurrentJoint
        {
            get { return current_joint; }
            set
            {
                current_joint = value;
                OnPropertyChanged("CurrentJoint");
            }
        }

        protected Vector3F velocity = new Vector3F(0.0f, 0.0f, 0.0f);
        protected bool just_changed = false;
        protected override void Loop(TimeSpan span)
        {
            if (m_myo.IsConnected)
            {
                if (m_myo.Accelerometer != null && m_myo.Gesture != null)
                {
                    if (is_updating)
                    {
                        velocity = velocity + (m_myo.Gyroscope) * (float)span.TotalSeconds * AccelerationScale;
                        var step_size = Math.Min(MaxVelocity, Math.Max(-MaxVelocity, (float)velocity.Z * StepSize));
                        CurrentState.Update(CurrentJoint, step_size);
                    }
                    else
                    {
                        velocity = new Vector3F(0, 0, 0);
                        if (m_myo.Gesture == 3 && just_changed == false)
                        {
                            just_changed = true;
                            current_joint--;
                            if (current_joint < 1)
                                current_joint = 5;
                            Console.WriteLine("switch to joint: {0}", current_joint);
                        }
                        else if (m_myo.Gesture == 5 && just_changed == false)
                        {
                            just_changed = true;
                            current_joint++;
                            current_joint = current_joint % 5 + 1;
                            Console.WriteLine("switch to joint: {0}", current_joint);
                        }
                        else if (m_myo.Gesture == 0)
                            just_changed = false;
                    }
                }
            }
        }



    }
}