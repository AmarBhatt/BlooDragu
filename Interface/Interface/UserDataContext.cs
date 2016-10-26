using System;

namespace Interface
{
    public class UserDataContext
    {
        static UserDataContext d_instance;
        public static UserDataContext GetInstance()
        {
            if (d_instance == null)
            {
                d_instance = new UserDataContext();
            }
            return d_instance;
        }

        public ArmState ArmState { get; set; }
        public ArmControl ArmControl { get; set; }
    }
}
