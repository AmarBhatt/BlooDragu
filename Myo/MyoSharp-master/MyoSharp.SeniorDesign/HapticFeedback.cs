using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

using MyoSharp.Communication;
using MyoSharp.Device;
//using MyoSharp.SeniorDesign.ConsoleHelper;
using MyoSharp.ConsoleSample.Internal;
using MyoSharp.Poses;
using MyoSharp.Exceptions;

namespace MyoSharp.SeniorDesign
{
    /// <summary>
    /// Currently this will give a short buzz if the users clenches their
    /// fist and a long buzz if their fingers are spread.
    /// </summary>
    internal class HapticFeedback
    {
        #region Methods
        private static void Main(string[] args)
        {
            // create a hub to manage Myos
            using (var channel = Channel.Create(
                ChannelDriver.Create(ChannelBridge.Create(),
                MyoErrorHandlerDriver.Create(MyoErrorHandlerBridge.Create()))))
            using (var hub = Hub.Create(channel))
            {
                // listen for when a Myo connects
                hub.MyoConnected += (sender, e) =>
                {
                    Console.WriteLine("Myo {0} has connected!", e.Myo.Handle);

                    // unlock the Myo so that it doesn't keep locking between our poses
                    e.Myo.Unlock(UnlockType.Hold);

                    // setup for the pose we want to watch for
                    var pose = HeldPose.Create(e.Myo, Pose.Fist, Pose.FingersSpread);

                    // set the interval for the event to be fired as long as 
                    // the pose is held by the user
                    pose.Interval = TimeSpan.FromSeconds(0.5);

                    pose.Start();
                    pose.Triggered += Pose_Triggered;
                };

                // start listening for Myo data
                channel.StartListening();

                ConsoleHelper.UserInputLoop(hub);
            }
        }
        #endregion

        #region Event Handlers
        private static void Pose_Triggered(object sender, PoseEventArgs e)
        {
            Console.WriteLine("{0} arm Myo is holding pose {1}!", e.Myo.Arm, e.Pose);
            if (e.Pose == Pose.Fist)
            {
                e.Myo.Vibrate(VibrationType.Short);
            }
            if (e.Pose == Pose.FingersSpread)
            {
                e.Myo.Vibrate(VibrationType.Long);
            }
        }
        #endregion
    }
}
