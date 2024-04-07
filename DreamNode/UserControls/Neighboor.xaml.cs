using DreamNode.Graph;
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

namespace DreamNode.UserControls
{
    /// <summary>
    /// Interaction logic for Neighboor.xaml
    /// </summary>
    public partial class Neighboor : UserControl
    {
        private List<Pool> pools;
        private PassageType direction;
        public string Visibility { get; set; }
        public Neighboor()
        {
            InitializeComponent();

        }

        public void Init(List<Pool> p, PassageType dir)
        {
            direction = dir;
            pools = p;
            Visibility = "Collapsed";
        }
    }
}
