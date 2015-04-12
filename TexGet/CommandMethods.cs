using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.ComponentModel;
    

namespace TexGet
{
    public class CommandMethods : DependencyObject
    {
        public static Char placeHolder = '•';

        private IEnumerable<String> commands;
        public CommandMethods()
        {
            _instance = this;
            var file = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/LatexCommands.txt")).Stream;
            var reader = new StreamReader(file, Encoding.UTF8);
            var list = new List<String>();
            while (!reader.EndOfStream)
            {
                var command = reader.ReadLine();
                if (command != String.Empty && !command.StartsWith("#"))
                    list.Add(command.Trim());
            }
            commands = list;
        }

        private static CommandMethods _instance;
        public static IEnumerable<String> allCommands()
        {
            return _instance.commands;
        }

        public static IEnumerable<String> getCommands(String filter)
        {
            if (filter == "")
                return allCommands();
            else
            {
                //suggestions with 'filter' at the beginning
                var best = allCommands().Where(c => c.Replace(placeHolder.ToString(), "").StartsWith(filter, StringComparison.OrdinalIgnoreCase));
                //suggestion that contain 'filter'
                var loose = allCommands().Where(c => c.Replace(placeHolder.ToString(), "").ToLower().Contains(filter.Remove(0, 1).ToLower()));
                //no duplicates
                loose = loose.Except(best);
                return best.Concat(loose);
            }
        }
    }

    public static class Extensions
    {
        public static Boolean BelongsToCommand(this Char c)
        {
            return Char.IsLetter(c) || c == '\\';
        }
    }
}
