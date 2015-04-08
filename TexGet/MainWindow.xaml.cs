using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using System.Diagnostics;
using System.IO;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Interop;

namespace TexGet
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            //Setup minimize trigger
            this.SourceInitialized += (o, e) =>
            {
                HwndSource source = (HwndSource)PresentationSource.FromVisual(this);
                source.AddHook(new HwndSourceHook(HandleMessages));
            };

            //Setup Hotkey Ctr-Shift-L
            hotKey.RegisterHotKey(cModifierKeys.Control | cModifierKeys.Shift, System.Windows.Forms.Keys.L);
            hotKey.KeyPressed += (o, e) =>
            {
                if (this.IsVisible)
                    this.Hide();
                else
                    open();
            };

            //Setup Icon
            myIcon.MouseClick += (o, e) => 
            {
                if (this.IsVisible)
                    this.Hide();
                else
                    open();
            };

            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/latex32.ico")).Stream;
            myIcon.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Dispose();

            var menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add(new System.Windows.Forms.MenuItem("Close",
                (o, e) =>
                {
                    Close();
                }));

            myIcon.ContextMenu = menu;
            myIcon.Visible = true;

            //Setup other stuff
            this.Hide();
            mathMode = true;
            LaText.Loaded += (o, e) => { innerCodeBox.Focus(); };         
        }

        #region Window
        private IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 0x0112 == WM_SYSCOMMAND, 'Window' command message.
            // 0xF020 == SC_MINIMIZE, command to minimize the window.
            if (msg == 0x0112 && ((int)wParam & 0xFFF0) == 0xF020)
            {
                // Cancel the minimize.
                handled = true;

                // Hide the form.
                this.Hide();
            }

            return IntPtr.Zero;
        } 

        private void position()
        {
            var desktopWorkingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            var taskbarLoc = GetTaskBarLocation();
            if (taskbarLoc == TaskBarLocation.TOP)
            {
                this.Left = desktopWorkingArea.Right - this.Width - 5;
                this.Top = desktopWorkingArea.Top + 5;
            }
            else if (taskbarLoc == TaskBarLocation.BOTTOM)
            {
                this.Left = desktopWorkingArea.Right - this.Width - 5;
                this.Top = desktopWorkingArea.Bottom - this.Height - 5;
            }
            else if (taskbarLoc == TaskBarLocation.LEFT)
            {
                this.Left = desktopWorkingArea.Left + 5;
                this.Top = desktopWorkingArea.Bottom - this.Height - 5;
            }
            else //Right
            {
                this.Left = desktopWorkingArea.Right - this.Width - 5;
                this.Top = desktopWorkingArea.Bottom - this.Height - 5;
            }

        }

        private void open()
        {
            position();
            this.Show();
            innerCodeBox.Focus();
        }

        System.Windows.Forms.NotifyIcon myIcon = new System.Windows.Forms.NotifyIcon();
        KeyboardHook hotKey = new KeyboardHook();
        private TextBox innerCodeBox { get { return (TextBox)LaText.Template.FindName("textSource", LaText); } }

        private Boolean _mathMode;
        private Boolean mathMode
        {
            get { return _mathMode; }
            set
            {
                _mathMode = value;
                this.Title = "ClipTex - " + (value ? "math mode" : "text mode");
                LaText.Tag = String.Format("Enter your Latex code here:{1} (e.g. {0}\\frac{{1}}{{2}} = 0.5{0} ) {1}Ctrl-Enter to render and copy to{1} clipboard {1}Ctrl-M to switch to {2} mode {1}Ctrl-N to clear {1}Ctrl-Shift-L to show/hide",
                    value ? "" : "$", Environment.NewLine, value ? "text" : "math");
            }
        }

        //private bool definiteClose = false;
        private void Close()
        {
            Application.Current.Shutdown();
        }

        private enum TaskBarLocation { TOP, BOTTOM, LEFT, RIGHT }

        private TaskBarLocation GetTaskBarLocation()
        {
            //System.Windows.SystemParameters....
            if (SystemParameters.WorkArea.Left > 0)
                return TaskBarLocation.LEFT;
            if (SystemParameters.WorkArea.Top > 0)
                return TaskBarLocation.TOP;
            if (SystemParameters.WorkArea.Left == 0
              && SystemParameters.WorkArea.Width < SystemParameters.PrimaryScreenWidth)
                return TaskBarLocation.RIGHT;
            return TaskBarLocation.BOTTOM;
        }

        #endregion

        #region Latex
        private void Render(bool mathMode)
        {
            String backColor = "white";
            String textColor = "black";
            String tex = prepareFormula(LaText.Text, backColor, textColor, mathMode);

            //Create temporary directory
            String tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            Directory.CreateDirectory(tempDir);

            //Create Latex file
            String fileName = "out.tex";
            String FilePath = Path.Combine(tempDir, fileName);
            System.IO.StreamWriter writer = new System.IO.StreamWriter(FilePath, false);

            //Write Latex file
            writer.Write(tex);
            writer.Flush();
            writer.Close();

            //Build Latex
            try
            {
                String imagePath = buildLatex(workingDirectory: tempDir, 
                                              inputFile: fileName, 
                                              format: "png", 
                                              resolution: "100x100", 
                                              AntiAlias: true, 
                                              Transparent: false);

                var bitmap = new BitmapImage();
                String error = null;

                try
                {
                    var stream = File.OpenRead(imagePath);
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    stream.Close();
                    stream.Dispose();

                    //Use BitmapImage
                    imgPreview.Source = bitmap;
                    Clipboard.SetImage(bitmap);
                }
                catch (Exception)
                {
                    throw new Exception("no image has been created");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //Clean Up
                System.IO.DirectoryInfo downloadedMessageInfo = new DirectoryInfo(tempDir);
                foreach (FileInfo file in downloadedMessageInfo.GetFiles())
                    file.Delete();
                Directory.Delete(tempDir);
            }
        }

        private string prepareFormula(string p, string backColor, string textColor, Boolean mathMode)
        {
            String tex = "\\documentclass[12pt]{article}" + "\r\n" +
                          "\\usepackage{color}" + "\r\n" +
                          "\\usepackage{amsmath}" + "\r\n" +
                          "\\usepackage[dvips]{graphicx}" + "\r\n" +
                          "\\pagestyle{empty}" + "\r\n";
            tex += "\\pagecolor{" + backColor + "}" + "\r\n" +
                   "\\begin{document}" + "\r\n" +
                   "{\\color{" + textColor + "}" + "\r\n";

            if (mathMode)
                tex += "\\begin{eqnarray*}" + "\r\n";

            tex += LaText.Text;

            if (mathMode)
                tex += "\\end{eqnarray*}}" + "\r\n";

            tex += "\\end{document}";
            return tex;
        }

        private String buildLatex(String workingDirectory, String inputFile, String format, String resolution, Boolean AntiAlias, Boolean Transparent, String transparentColor = "")
        {
            String noExtName = Path.GetFileNameWithoutExtension(inputFile);

            String latexPath = fromPATH("latex.exe");
            ProcessStartInfo latexInfo = new ProcessStartInfo(latexPath, "-interaction=batchmode " + inputFile);
            latexInfo.WorkingDirectory = workingDirectory;
            latexInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                Process.Start(latexInfo).WaitForExit();
            }
            catch (Exception)
            {
                throw new Exception("Error in latex.exe");
            }
            if (!File.Exists(Path.Combine(workingDirectory, noExtName + ".dvi"))) throw new Exception("latex did not work properly");

            String dvipsPath = fromPATH("dvips.exe");
            ProcessStartInfo dvipsInfo = new ProcessStartInfo(dvipsPath, "-o " + noExtName + ".eps -E " + noExtName + ".dvi");
            dvipsInfo.WorkingDirectory = workingDirectory;
            dvipsInfo.UseShellExecute = false;
            dvipsInfo.CreateNoWindow = true;
            dvipsInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                Process.Start(dvipsInfo).WaitForExit();
            }
            catch (Exception)
            {
                throw new Exception("Error in dvips.exe");
            }
            if (!File.Exists(Path.Combine(workingDirectory, noExtName + ".eps"))) throw new Exception("dvips did not work properly");

            String convertPath = fromPATH("convert.exe");
            ProcessStartInfo convertInfo = new ProcessStartInfo(convertPath, "+adjoin " + (AntiAlias ? "-" : "+") + "antialias " + (Transparent ? "-transparent " + transparentColor : "") + "-density " + resolution + " " + noExtName + ".eps " + noExtName + "." + format);
            convertInfo.WorkingDirectory = workingDirectory;
            convertInfo.CreateNoWindow = true;
            convertInfo.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                Process.Start(convertInfo).WaitForExit();
            }
            catch (Exception)
            {
                throw new Exception("Error in convert.exe");
            }
            if (!File.Exists(Path.Combine(workingDirectory, noExtName + "." + format))) throw new Exception("convert did not work properly");

            return Path.Combine(workingDirectory, noExtName + "." + format);
        }

        private String fromPATH(string path)
        {
            var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");
            //Console.WriteLine(enviromentPath);
            var paths = enviromentPath.Split(';');
            var exePath = paths.Select(x => Path.Combine(x, path))
                               .Where(x => File.Exists(x))
                               .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(exePath) == false)
            {
                return exePath;
            }
            throw new Exception(path + ": not found");
            //MessageBox.Show(path + ": not found");
            //return null;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            hotKey.Dispose();
            myIcon.Visible = false;
        }
        #endregion

        #region IDE

        private async void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //jump between placeholders
            if (e.Key == Key.Tab)
            {
                //jump to left
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (innerCodeBox.CaretIndex > 0)
                    {
                        int last = innerCodeBox.Text.LastIndexOf(CommandMethods.placeHolder, innerCodeBox.CaretIndex - 1);
                        if (last > -1)
                            innerCodeBox.Select(last, 1);
                    }
                }
                else
                {
                    //jump to right
                    int searchposition = innerCodeBox.SelectedText == CommandMethods.placeHolder.ToString() ?
                        innerCodeBox.CaretIndex + 1 : innerCodeBox.CaretIndex;
                    int next = innerCodeBox.Text.IndexOf(CommandMethods.placeHolder, searchposition); 
                    if (next > -1)
                        innerCodeBox.Select(next, 1);
                    
                    //Or insert a short tab if not the last placeholder is selected
                    else if (innerCodeBox.SelectedText != CommandMethods.placeHolder.ToString()) 
                    {
                        String tab = new String(' ', 2);
                        InsertLatex(tab, tab.Length);
                    }
                }
                e.Handled = true;
            }
            //Render latex png
            else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                String error = null;
                try
                {
                    LaText.IsEnabled = false;
                    Render(mathMode);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    LaText.IsEnabled = true;
                }
                if (error != null) await this.ShowMessageAsync("Error", error, MessageDialogStyle.Affirmative);
                innerCodeBox.Focus();
            }
            //Clear all
            else if (e.Key == Key.N && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                LaText.Clear();
            }
            //Toggle math mode
            else if (e.Key == Key.M && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                mathMode = !mathMode;
            }
            //Select suggested entries - up, down
            else if ((e.Key == Key.Up || e.Key == Key.Down) && suggestionPopup.IsOpen)
            {
                suggestionList.SelectedIndex = e.Key == Key.Up ?
                    Math.Max(0, suggestionList.SelectedIndex - 1) :
                    Math.Min(suggestionList.SelectedIndex + 1, suggestionList.Items.Count - 1);
                suggestionList.ScrollIntoView(suggestionList.SelectedItem);
                InsertSelecetion(false);
                e.Handled = true;
            }
            //Ctrl-Space -> open autocomplete list
            else if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                int lastSlash = innerCodeBox.Text.LastIndexOf('\\', innerCodeBox.CaretIndex - 1);
                //Debug.WriteLine(lastSlash, "lastSlash");

                if (countNumberOfSlashesBefore(lastSlash + 1) % 2 == 1)
                {
                    Debug.WriteLine(getTypingCommand());
                    RefreshSuggestions(getTypingCommand());
                    suggestionPopup.PlacementTarget = innerCodeBox;
                    suggestionPopup.PlacementRectangle = innerCodeBox.GetRectFromCharacterIndex(
                        innerCodeBox.CaretIndex, true);
                    suggestionPopup.IsOpen = true;
                }
                e.Handled = true;
            }
            //Accept selected suggestion
            else if ((e.Key == Key.Enter || e.Key == Key.Space) && suggestionPopup.IsOpen)
            {
                InsertSelecetion();
                if (e.Key == Key.Enter) e.Handled = true;
            }
            //Abort suggestions
            else if (e.Key == Key.Escape && suggestionPopup.IsOpen)
            {
                suggestionPopup.IsOpen = false;
            }
            //Back key pressed
            else if (e.Key == Key.Back)
            {
                //Close suggestion poupup if '\' was removed 
                if (innerCodeBox.CaretIndex > 0 && innerCodeBox.Text[innerCodeBox.CaretIndex - 1] == '\\')
                    suggestionPopup.IsOpen = false;

                //If still open, refresh suggestions
                if (suggestionPopup.IsOpen)
                {
                    String command = getTypingCommand();
                    command = command.Substring(0, command.Length - 1);
                    RefreshSuggestions(command);
                    Debug.WriteLine(command);
                }
            }
            //Left key pressed
            else if (e.Key == Key.Left)
            {
                if (innerCodeBox.CaretIndex > 0 && innerCodeBox.Text[innerCodeBox.CaretIndex - 1] == '\\')
                    suggestionPopup.IsOpen = false;
            }
            //Right key pressed
            else if (e.Key == Key.Right && innerCodeBox.Text.Length > innerCodeBox.CaretIndex)
            {
                char next = innerCodeBox.Text[innerCodeBox.CaretIndex];
                if (new Char[] { '\\', ' ' }.Contains(next))
                    suggestionPopup.IsOpen = false;
            }
        }

        private void LaText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Autimatically close open brackets
            if (e.Text == "{")
            {
                InsertLatex("{}", 1);
                e.Handled = true;
            }
            else if (e.Text == "(")
            {
                InsertLatex("()", 1);
                e.Handled = true;
            }
            else if (e.Text == "[")
            {
                InsertLatex("[]", 1);
                e.Handled = true;
            }
            else if (e.Text == ")" || e.Text == "]" || e.Text == "}")
            {
                if (innerCodeBox.Text.Length > innerCodeBox.CaretIndex && innerCodeBox.Text[innerCodeBox.CaretIndex] == e.Text[0])
                {
                    e.Handled = true;
                    innerCodeBox.CaretIndex += 1;
                }
                    
            }
            //Refresh suggestions if needed
            if (suggestionPopup.IsOpen)
            {
                Debug.WriteLine(getTypingCommand() + e.Text);
                //CommandMethods.getCommands(getTypingCommand() + e.Text);
                RefreshSuggestions(getTypingCommand() + e.Text);
            }
            //If linebreak was entered, close suggestion popup
            if (e.Text == "\\")
            {
                //count occurences of double '\' - linebreaks
                if (countNumberOfSlashesBefore(innerCodeBox.CaretIndex) % 2 == 0)
                {
                    //if character after '\' is alreadey text, then it belongs to a different command. a space is needed.
                    if (innerCodeBox.Text.Length > innerCodeBox.CaretIndex && Char.IsLetterOrDigit(innerCodeBox.Text[innerCodeBox.CaretIndex]))
                        InsertLatex(" ", 0);
                    RefreshSuggestions("");
                    suggestionPopup.PlacementTarget = innerCodeBox;
                    suggestionPopup.PlacementRectangle = innerCodeBox.GetRectFromCharacterIndex(
                        innerCodeBox.CaretIndex, true);
                    suggestionPopup.IsOpen = true;
                }
                else
                {
                    suggestionPopup.IsOpen = false;
                }                
            }
        }

        /// <summary>
        /// returns the commmand the user is currently writing. In this implementation it returs all the "text" after a '\'
        /// </summary>
        /// <returns>returns the so far typed command</returns>
        private string getTypingCommand()
        {
            int caretIndex = innerCodeBox.CaretIndex;
            int begin = innerCodeBox.Text.LastIndexOf('\\', caretIndex - 1);
            if (countNumberOfSlashesBefore(begin) % 2 == 1) begin -= 1;

            return innerCodeBox.Text.Substring(begin, lengthOfCommand(begin));
            
        }

        /// <summary>
        /// returns the length of a command after a certain position.
        /// </summary>
        /// <param name="begin">start position for checking characters after '\'</param>
        /// <returns>number of characters</returns>
        private int lengthOfCommand(int begin)
        {
            int i = 0;
            while (begin + i < innerCodeBox.Text.Length && innerCodeBox.Text[begin + i].BelongsToCommand())
            {
                i++;
            }
            return i;
        }       
        
        /// <summary>
        /// counts the number of consecutive '\' on the left side of 'begin'
        /// </summary>
        /// <param name="begin">start position to search for '\'</param>
        /// <returns></returns>
        private int countNumberOfSlashesBefore(int begin)
        {
            int i;
            for (i = begin - 1; i >= 0 && innerCodeBox.Text[i] == '\\'; i--) { };
            i = begin - 1 - i;
            return i;
        }

        /// <summary>
        /// Updates the suggestion list
        /// </summary>
        /// <param name="filter">the so far typed command, or "" for all commands</param>
        private void RefreshSuggestions(String filter)
        {
            var suggestions = CommandMethods.getCommands(filter).ToList();
            if (suggestions.Count > 0)
            {
                suggestionList.ItemsSource = suggestions;
                suggestionList.SelectedIndex = 0;
            }
            else
            {
                suggestionPopup.IsOpen = false;
                suggestionList.ItemsSource = CommandMethods.allCommands();
            }
        }

        /// <summary>
        /// Inserts text and moves caret
        /// </summary>
        /// <param name="text">the text to insert</param>
        /// <param name="moveCaret">amount of positions the caret should be moved forward</param>
        private void InsertLatex(String text, int moveCaret)
        {
            if (moveCaret < 0) throw new Exception("only positive numbers and zero allowed");
            int caretIndex = innerCodeBox.CaretIndex;
            innerCodeBox.Text = innerCodeBox.Text.Insert(caretIndex, text);
            innerCodeBox.CaretIndex = caretIndex + moveCaret;
        }

        /// <summary>
        /// inserts latex commands at the beginning of the last '\'. Can be used for preview or for final insertion.
        /// </summary>
        /// <param name="final">preview or final</param>
        private void InsertSelecetion(Boolean final = true)
        {
            if (final) suggestionPopup.IsOpen = false;

            string command = suggestionList.SelectedItem.ToString();
            int firstPlaceholder = command.IndexOf(CommandMethods.placeHolder);

            int caretIndex = innerCodeBox.CaretIndex;
            //position for removing an reinserting
            int begin = innerCodeBox.Text.LastIndexOf('\\', caretIndex - 1);
            //if newline '\\' was selected, both slashes have to be removed if the insertion of '\\' was not final but preview and the user selected a new command
            if (countNumberOfSlashesBefore(begin) % 2 == 1) begin -= 1;

            innerCodeBox.Text = innerCodeBox.Text.Remove(begin, caretIndex - begin);
            innerCodeBox.Text = innerCodeBox.Text.Insert(begin, command);
            innerCodeBox.CaretIndex = begin + command.Length;
            
            //add end{} for begin{}
            if (final && command.StartsWith("\\begin{"))
            {
                String beginner = command.Substring("\\begin{".Length, command.Length - "\\begin{".Length - 1);
                String ender = "\\end{" + beginner + "}";
                innerCodeBox.Text = innerCodeBox.Text.Insert(innerCodeBox.CaretIndex, ender);
                if (firstPlaceholder == -1) innerCodeBox.CaretIndex = begin + command.Length;
            }

            //select first placeholder
            if (final && firstPlaceholder > 0) innerCodeBox.Select(begin + firstPlaceholder, 1);
            if (final) innerCodeBox.Focus();
        }

        //If user selects the listbox instead of the textField by accident
        private void OnListKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
        	if (e.Key == Key.Enter)
            {
                InsertSelecetion();
            }
            else if (e.Key == Key.Escape)
            {
                suggestionPopup.IsOpen = false;
                innerCodeBox.Focus();
            }
        }

        private void suggestionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (suggestionList.SelectedIndex >= 0)
            {
                InsertSelecetion();
            }
        }
#endregion
    }
}
