using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public abstract class IControlTheory
    {
        MyoControl m_myo;
        ArmControl m_arm;

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
        }

        public virtual void Detach()
        {
            m_myo = null;
            m_arm = null;
            m_attached = null;
        }
    }

    public class StartStopControl : IControlTheory
    {
        public TimeSpan Tick { get; set; }
        public ArmState CurrentState { get; private set; }
        bool m_running;

        Task m_task;

        public StartStopControl()
        {
            Tick = TimeSpan.FromMilliseconds(100);
            CurrentState = new ArmState();
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
                while(!MyoControl.IsConnected)
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
                        Console.WriteLine(string.Join(" ", CurrentState.Angles));
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
}
