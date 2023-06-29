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

namespace TimePicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            tpc2.WeekendEnabled = false;

            tpc3.WeekendEnabled= false;
            tpc3.SetEnabledTimeRange(DateTime.Now.Date.AddDays(-7), DateTime.Now.Date.AddDays(3));
            tpc3.SetDisableTimeRange(DateTime.Now.Date.AddDays(-3), DateTime.Now.Date.AddDays(-1));
        }
    }
}
