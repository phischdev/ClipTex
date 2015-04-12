using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace TexGet
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            #if DEBUG
                ClipTex.Properties.Settings.Default.Reset();
            #endif

            if (ClipTex.Properties.Settings.Default.Präambel == String.Empty)
                ClipTex.Properties.Settings.Default.Präambel = ClipTex.Properties.Resources.StandardPräambel;
            if (ClipTex.Properties.Settings.Default.pngResolution == -1)
                ClipTex.Properties.Settings.Default.pngResolution = Convert.ToInt32(ClipTex.Properties.Resources.DefaultResolution);
        }
    }
}
