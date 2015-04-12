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
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace ClipTex
{
    /// <summary>
    /// Interaktionslogik für SettingsWindows.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            txtPräambel.Text = ClipTex.Properties.Resources.StandardPräambel;
            slider.Value = Convert.ToInt32(ClipTex.Properties.Resources.DefaultResolution);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClipTex.Properties.Settings.Default.pngResolution = (int)slider.Value;
            ClipTex.Properties.Settings.Default.Präambel = txtPräambel.Text;
            ClipTex.Properties.Settings.Default.Save();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            slider.Value = ClipTex.Properties.Settings.Default.pngResolution;
            txtPräambel.Text = ClipTex.Properties.Settings.Default.Präambel;
        }
    }
}
