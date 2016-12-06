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
            CurrentState.OnLimitReached += CurrentState_OnLimitReached;
        }

        private void CurrentState_OnLimitReached(object sender, EventArgs e)
        {
            m_myo.Vibrate();
        }

        virtual protected void OnPoseChanged(object sender, EventArgs e)
        {
        }

        public virtual void Detach()
        {
            CurrentState.OnLimitReached -= CurrentState_OnLimitReached;
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

        protected TimeSpan claw_interval = TimeSpan.FromSeconds(1);
        protected DateTime last_claw = DateTime.UtcNow;
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

        public override void Attach(MyoControl MyoControl, ArmControl ArmControl)
        {
            base.Attach(MyoControl, ArmControl);
            m_running = true;
            m_update_interval = TimeSpan.FromMilliseconds(150);
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

        virtual protected void Setup()
        {
            while (m_myo.IsConnected == false)
                Task.Delay(100).Wait();
        }
        abstract protected void Loop(TimeSpan span);

        virtual protected void SendUpdate()
        {
            m_arm.SendPosition(CurrentState);
            //CurrentState.Print();
        }

        public override void Detach()
        {
            m_running = false;
            m_arm_task.Wait();
            m_compute_task.Wait();
            base.Detach();
        }

        public TimeSpan lock_interval = TimeSpan.FromSeconds(1);
        protected DateTime last_lock_change = DateTime.UtcNow;
        protected override void OnPoseChanged(object sender, EventArgs e)
        {
            base.OnPoseChanged(sender, e);

            if (m_myo.Gesture == 4 && DateTime.UtcNow.Subtract(last_claw) > claw_interval)
            {
                CurrentState.ToggleClaw();
                Console.WriteLine("Claw toggled");
                last_claw = DateTime.UtcNow;
            }
            else if (m_myo.Gesture == 5 && DateTime.UtcNow.Subtract(last_lock_change) > lock_interval)
            {
                LockState = !LockState;
                last_lock_change = DateTime.UtcNow;
            }
        }
    }

    public class StartStopControl : PoolingControlTheory
    {
        public StartStopControl()
        {
            MaxVelocity = 0.00001f;
            AccelerationScale = 0.01f;
            StepSize = 0.0001f;
        }

        public TimeSpan joint_interval = TimeSpan.FromSeconds(1);
        protected DateTime last_joint_change = DateTime.UtcNow;
        protected override void OnPoseChanged(object sender, EventArgs e)
        {
            base.OnPoseChanged(sender, e);

            if (m_myo.Gesture == 2 && LockState == false)
            {
                velocity = new Vector3F(0, 0, 0);
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
                    if (CurrentJoint > 5)
                        CurrentJoint = 1;
                    Console.WriteLine("switch to joint: {0}", CurrentJoint);
                }
            }
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
                }
            }
        }
    }

    public class AdvancedControl : PoolingControlTheory
    {
        public AdvancedControl()
        {
            MaxVelocity = 0.05f;
            AccelerationScale = 0.0005f;
            StepSize = 1.0f;
        }
        public float MaxVelocity { get; set; }
        public float AccelerationScale { get; set; }
        public float StepSize { get; set; }


        public int SecondJoint = 1;
        private DateTime LastJointChange = DateTime.UtcNow;
        protected override void OnPoseChanged(object sender, EventArgs e)
        {
            base.OnPoseChanged(sender, e);

            if(m_myo.Gesture == 2 && DateTime.UtcNow.Subtract(LastJointChange) > TimeSpan.FromSeconds(2))
            {
                LastJointChange = DateTime.UtcNow;
                if (SecondJoint == 1)
                    SecondJoint = 2;
                else
                    SecondJoint = 1;
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
                        var minVelocity = 0.01f;
                        velocity = velocity + (m_myo.Gyroscope) * (float)span.TotalSeconds * AccelerationScale;
                        var step_size_x = Math.Min(MaxVelocity, Math.Max(-MaxVelocity, (float)velocity.X * StepSize));
                        var step_size_y = Math.Min(MaxVelocity, Math.Max(-MaxVelocity, (float)velocity.Y * StepSize));
                        var step_size_z = Math.Min(MaxVelocity, Math.Max(-MaxVelocity, (float)velocity.Z * StepSize));

                        if (Math.Abs(step_size_x) > Math.Abs(step_size_y) && Math.Abs(step_size_x) > Math.Abs(step_size_z))
                        {
                            if (Math.Abs(step_size_x) > minVelocity)
                                CurrentState.Update(1, -step_size_x);
                        }
                        else if (Math.Abs(step_size_y) > Math.Abs(step_size_z))
                        {
                            if (Math.Abs(step_size_y) > 0.005f)
                            {
                                if (SecondJoint == 1)
                                {
                                    CurrentState.Update(3, -step_size_y);
                                    CurrentState.Update(4, 0.6f * step_size_y);
                                }
                                else
                                    CurrentState.Update(SecondJoint, -step_size_y);
                            }
                        }
                        else
                        {
                            if (Math.Abs(step_size_z) > minVelocity)
                                CurrentState.Update(5, step_size_z);
                        }
                        Task.Delay(10).Wait();
                    }
                    else
                        velocity = new Vector3F(0.0f, 0.0f, 0.0f);
                }
            }
        }
    }

}