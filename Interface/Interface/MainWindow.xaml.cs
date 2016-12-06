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

                var theory = new AdvancedControl();
                theory.PropertyChanged += Theory_PropertyChanged;
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

        private void Theory_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            int i;
            Label[] labels = { Claw, Wrist1, Wrist2, Elbow, Shoulder, Base };

            for(i = 0;i < 6; i++)
            {
                if (theory[i])
                {
                    labels[i].FontWeight = FontWeights.Bold;
                }else
                {
                    labels[i].FontWeight = FontWeights.Normal;
                }
            }
        }

        MyoControl m_myo;
        ArmControl m_arm;
        ArmState s_arm;
        AdvancedControl theory;

        private void goHome(object sender, RoutedEventArgs e)
        {
            //Joint1.Text = "7";
            //Joint3.Text = "7";
            Joint0.IsChecked = true;
            s_arm.Home();
            
        }


        private void radioButtons_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            int buttonid = (int)radioButton.Tag;
            switch (buttonid)
            {
                case 0:
                    theory.StepSize = 0.5f;
                    break;
                case 1:
                    theory.StepSize = 1;
                    break;
                case 2:
                    theory.StepSize = 2;
                    break;
                default:
                    break;
            }
        }

    }
}
