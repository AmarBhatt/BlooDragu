using MyoSharp.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public abstract class IControlTheory
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

    public class StartStopControl : IControlTheory
    {
        public TimeSpan Tick { get; set; }
        bool m_running;

        Task m_task;

        public StartStopControl()
        {
            Tick = TimeSpan.FromMilliseconds(100);
        }

        public const float REST_WINDOW = 0.10f;
        public const float STEP_SIZE = 0.5f;

        public override void Attach(MyoControl MyoControl, ArmControl ArmControl)
        {
            base.Attach(MyoControl, ArmControl);

            m_running = true;
            m_task = Task.Run(async () =>
            {
                int current_joint = 0;
                bool just_changed = false;
                while (!MyoControl.IsConnected)
                    Console.WriteLine("Armband Not connected");
                Console.WriteLine("Hold vertically!!!");
                await Task.Delay(5000);
                var midpoint = MyoControl.Accelerometer[2];
                while (m_running)
                {
                    if (MyoControl.IsConnected)
                    {
                        //Console.WriteLine("MyoBand Accelerometer (midpoint: {0}):", midpoint);
                        //Console.WriteLine(string.Join(" ", MyoControl.Accelerometer));

                        if (MyoControl.Accelerometer != null && MyoControl.Gesture != null)
                        {
                            if (MyoControl.Gesture == 3 && just_changed == false)
                            {
                                just_changed = true;
                                current_joint--;
                                if (current_joint < 0)
                                    current_joint = 5;
                            }
                            else if (MyoControl.Gesture == 2 && just_changed == false)
                            {
                                just_changed = true;
                                current_joint++;
                                current_joint = current_joint % 6;
                            }
                            else if (MyoControl.Gesture == 0)
                                just_changed = false;
                            if (MyoControl.Accelerometer[2] < midpoint - REST_WINDOW)
                                CurrentState.Update(current_joint, -STEP_SIZE);
                            else if (MyoControl.Accelerometer[2] > midpoint + REST_WINDOW)
                                CurrentState.Update(current_joint, STEP_SIZE);
                        }

                        ArmControl.SendPosition(CurrentState);
                        Console.WriteLine("Arm position (Pose: {0}, joint: {1})", MyoControl.Gesture, current_joint);
                        CurrentState.Print();
                    }
                    else
                        Console.WriteLine("Armband Not connected");


                    await Task.Delay(Tick);
                }
            });
        }

        public override void Detach()
        {
            m_running = false;
            m_task.Wait();
            base.Detach();
        }
    }


    public class BetterControl : PoolingControlTheory, INotifyPropertyChanged
    {
        public BetterControl()
        {
        }
        public event PropertyChangedEventHandler PropertyChanged;
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
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        public TimeSpan claw_interval = TimeSpan.FromSeconds(1);
        protected DateTime last_claw = DateTime.UtcNow;
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
        }
        protected override void Setup()
        {
            base.Setup();

            while (m_myo.IsConnected == false)
                Task.Delay(100).Wait();

            rest_acc = m_myo.Accelerometer;
        }
        protected Vector3F rest_acc;
        public Vector3F velocity = new Vector3F(0.0f, 0.0f, 0.0f);
        private float maxVel = 0.00001f;
        private float acc = 0.01f;
        private float step_size = 0.0001f;
        public int current_joint = 1;
        private bool just_changed = false;

        public float MAX
        {
            get { return maxVel; }
            set
            {
                maxVel = value;
            }
        }
        public float ACC
        {
            get { return acc; }
            set
            {
                acc = value;
            }
        }

        public float STEP_SIZE
        {
            get { return step_size; }
            set
            {
                step_size = value;
            }
        }

        //protected override void Loop(TimeSpan span)
        //{
        //    if (m_myo.IsConnected)
        //    {
        //        if (m_myo.Accelerometer != null && m_myo.Gesture != null)
        //        {
        //            if (is_updating)
        //            {
        //                velocity = velocity + (m_myo.Gyroscope) * (float)span.TotalSeconds * ACC;
        //                var max = maxVel;
        //                var step_size = Math.Min(max, Math.Max(-max, (float)velocity.Z * STEP_SIZE));
        //                CurrentState.Update(3, step_size);
        //            }
        //            else
        //                velocity = new Vector3F(0, 0, 0);
        //            //Console.WriteLine("X:{0} Y:{1} Z:{2}", velocity.X, velocity.Y, velocity.Z);
        //            //CurrentState.Update(4, -0.3f* step_size);
        //        }
        //    }
        //}

        protected override void Loop(TimeSpan span)
        {
            if (m_myo.IsConnected)
            {
                if (m_myo.Accelerometer != null && m_myo.Gesture != null)
                {
                    if (is_updating)
                    {
                        velocity = velocity + (m_myo.Gyroscope) * (float)span.TotalSeconds * ACC;
                        var max = maxVel;
                        var step_size = Math.Min(max, Math.Max(-max, (float)velocity.Z * STEP_SIZE));
                        CurrentState.Update(current_joint, step_size);
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
                        
                    //Console.WriteLine("X:{0} Y:{1} Z:{2}", velocity.X, velocity.Y, velocity.Z);
                    //CurrentState.Update(4, -0.3f* step_size);
                }
            }
        }



    }
}


/*
 * {
                    if (MyoControl.IsConnected)
                    {
                        //Console.WriteLine("MyoBand Accelerometer (midpoint: {0}):", midpoint);
                        //Console.WriteLine(string.Join(" ", MyoControl.Accelerometer));

                        if (MyoControl.Accelerometer != null && MyoControl.Gesture != null)
                        {
                            if (MyoControl.Gesture == 3 && just_changed == false)
                            {
                                just_changed = true;
                                current_joint--;
                                if (current_joint < 0)
                                    current_joint = 5;
                            }
                            else if (MyoControl.Gesture == 2 && just_changed == false)
                            {
                                just_changed = true;
                                current_joint++;
                                current_joint = current_joint % 6;
                            }
                            else if (MyoControl.Gesture == 0)
                                just_changed = false;
                            if (MyoControl.Accelerometer[2] < midpoint - REST_WINDOW)
                                CurrentState.Update(current_joint, -STEP_SIZE);
                            else if (MyoControl.Accelerometer[2] > midpoint + REST_WINDOW)
                                CurrentState.Update(current_joint, STEP_SIZE);
                        }

                        ArmControl.SendPosition(CurrentState);
                        Console.WriteLine("Arm position (Pose: {0}, joint: {1})", MyoControl.Gesture, current_joint);
                        CurrentState.Print();
                    }
                    else
                        Console.WriteLine("Armband Not connected");


                    await Task.Delay(Tick);
                }
*/