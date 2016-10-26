using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try
            {                
                s_arm = ArmState.GetInstance();
                m_arm = ArmControl.GetInstance();

                var ports = ArmControl.ListPortNames();
                m_arm.Open("COM4");

                m_myo = MyoControl.GetInstance();

                var theory = new BetterControl();
                theory.Attach(m_myo, m_arm);

                this.DataContext = new
                {
                    s_arm = s_arm,
                    m_arm = m_arm,
                    theory = theory
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }
        MyoControl m_myo;
        ArmControl m_arm;
        ArmState s_arm;


    }
}
