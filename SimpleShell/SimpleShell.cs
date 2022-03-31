using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleFileSystem;

namespace SimpleShell
{
    public class SimpleShell : Shell
    {
        private abstract class Cmd
        {
            private string name;
            private SimpleShell shell;

            public Cmd(string name, SimpleShell shell) { this.name = name; this.shell = shell; }

            public string Name => name;
            public SimpleShell Shell => shell;
            public Session Session => shell.session;
            public Terminal Terminal => shell.session.Terminal;
            public FileSystem FileSystem => shell.session.FileSystem;
            public SecuritySystem SecuritySystem => shell.session.SecuritySystem;

            abstract public void Execute(string[] args);
            virtual public string HelpText { get { return ""; } }
            virtual public void PrintUsage() { Terminal.WriteLine("Help not available for this command"); }
        }

        private Session session;
        private Directory cwd;                  // current working dir
        private Dictionary<string, Cmd> cmds;   // name -> Cmd
        private bool running;

        public SimpleShell(Session session)
        {
            this.session = session;
            cwd = null;
            cmds = new Dictionary<string, Cmd>();
            running = false;

            AddCmd(new ExitCmd(this));
            AddCmd(new PwdCmd(this));
            AddCmd(new HelpCmd(this));
            AddCmd(new LsCmd(this));
            AddCmd(new CdCmd(this));

            
        }

        private void AddCmd(Cmd c) { cmds[c.Name] = c; }

        public void Run(Terminal terminal)
        {
            // NOTE: takes over the current thread, returns only when shell exits
            // expects terminal to already be connected

            // set the initial current working directory
            cwd = session.HomeDirectory;

            // main loop...
            running = true;
            while (running)
            {
                // print command prompt
                terminal.Write(cwd.FullPathName + ">");

                // get command line
                terminal.Echo = true;
                string cmdLine = terminal.ReadLine();

                // identify and execute command
                string[] args = cmdLine.Split(' ');
                string cmdName = args[0];
                if (cmds.ContainsKey(cmdName))
                {
                    Cmd cmd = cmds[cmdName];
                    cmd.Execute(args);
                }
                else
                {
                    terminal.WriteLine("Unknown Command!");
                }
            }
        }

        #region commands

        // example command: exit
        private class ExitCmd : Cmd
        {
            public ExitCmd(SimpleShell shell) : base("exit", shell) { }

            public override void Execute(string[] args)
            {
                Terminal.WriteLine("Bye!");
                Shell.running = false;
            }

            override public string HelpText { get { return "Exits shell"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: exit");
            }
        }

        private class PwdCmd : Cmd
        {
            public PwdCmd(SimpleShell shell) : base("pwd", shell) { }

            public override void Execute(string[] args)
            {
                if (args.Length == 1)
                {
                    // get dir & throw it at the terminal
                    Terminal.WriteLine(Shell.cwd.FullPathName);
                }
                else
                {
                    Terminal.WriteLine("Error: Too many arguments!");
                    PrintUsage();
                }
            }

            override public string HelpText { get { return "Prints the name of the current working directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: pwd");
            }
        }

        private class HelpCmd : Cmd
        {
            public HelpCmd(SimpleShell shell) : base("help", shell) { }

            public override void Execute(string[] args)
            {
                // help - prints command list
                // help <cmdname> - print help text for cmd
                if (args.Length == 1)
                {
                    // prints command list
                    foreach (Cmd cmd in Shell.cmds.Values)
                    {
                        Terminal.WriteLine(cmd.Name + " - " + cmd.HelpText);
                    }                    
                }
                else if (args.Length == 2)
                {
                    // print help text for args[1] command
                    if (Shell.cmds.ContainsKey(args[1]))
                    {
                        Cmd cmd = Shell.cmds[args[1]];
                        Terminal.WriteLine(cmd.Name + " - " + cmd.HelpText);
                        cmd.PrintUsage();
                    }
                    else
                    {
                        // command not found
                        Terminal.WriteLine("Error: Command not found!");
                    }
                }
                else
                {
                    Terminal.WriteLine("Error: Unknown arguments!");
                    PrintUsage();
                }
            }

            override public string HelpText { get { return "Prints shell command info"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: help [command name]");
                Terminal.WriteLine("    Prints list of valid commands");
                Terminal.WriteLine("    command name - prints help text for command");
            }
        }

        private class LsCmd : Cmd
        {
            public LsCmd(SimpleShell shell) : base("ls", shell) { }

            public override void Execute(string[] args)
            {
                // ls - prints contents of current dir
                // ls <dir> - print contents of arg dir

                // error check for too many args
                if (args.Length > 2)
                {
                    Terminal.WriteLine("Error: Too many Arguments!");
                    PrintUsage();
                    return;
                }

                Directory dir = Shell.cwd;

                if (args.Length == 2)
                {
                    string dirName = args[1];

                    if (dirName[0] != '/')
                    {
                        string cwdPath = Shell.cwd.FullPathName;
                        if (cwdPath.Last() != '/')
                        {
                            cwdPath += '/';                        
                        }

                        dirName = cwdPath + dirName;
                    }

                    // get dir
                    dir = Shell.session.FileSystem.Find(dirName) as Directory;

                    // check if dir exists
                    if (dir == null)
                    {
                        Terminal.WriteLine("Error: directory not found!");
                        return;
                    }
                }

                    // prints everything in dir                    
                foreach (Directory dirs in dir.GetSubDirectories())
                {
                    Terminal.WriteLine(dirs.Name + "/");
                }

                foreach (File fil in dir.GetFiles())
                {
                    Terminal.WriteLine(fil.Name);
                }
                
            }
            override public string HelpText { get { return "Prints the contents of current or specified directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: ls [dir]");
                Terminal.WriteLine("    Prints contents of current working dir");
                Terminal.WriteLine("    dir - prints contents of named directory");
                Terminal.WriteLine("          dir is full path or partial path from current working directory");
            }
        }

        private class CdCmd : Cmd
        {
            public CdCmd(SimpleShell shell) : base("cd", shell) { }

            public override void Execute(string[] args)
            {
                // cd - go to home dir
                // cd <dir> - change cwd to named dir
                //    <dir> full path
                //    <dir> partial path
                // cd .. - go to parent of current dir

                // error check for too many args
                if (args.Length > 2)
                {
                    Terminal.WriteLine("Error: Too many Arguments!");
                    PrintUsage();
                    return;
                }

                Directory dir = Shell.session.HomeDirectory;

                if (args.Length == 2)
                {
                    string dirName = args[1];

                    if (dirName == "..")
                    {
                        if (Shell.cwd.Parent != null)
                        {
                            dir = Shell.cwd.Parent;
                        }
                        else
                        {
                            Terminal.WriteLine("Error! No parent directory.");
                            return;
                        }
                    }
                    else
                    {

                        if (dirName[0] != '/')
                        {
                            // append partial to cwd
                            string cwdPath = Shell.cwd.FullPathName;
                            if (cwdPath.Last() != '/')
                            {
                                cwdPath += '/';
                            }

                            dirName = cwdPath + dirName;
                        }

                        // get dir
                        dir = Shell.session.FileSystem.Find(dirName) as Directory;

                        // check if dir exists
                        if (dir == null)
                        {
                            Terminal.WriteLine("Error: directory not found!");
                            return;
                        }
                    }
                }

                // set current dir to named dir                    
                Shell.cwd = dir;

            }
            override public string HelpText { get { return "Changes current working directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: cd [dir] | [..]");
                Terminal.WriteLine("    Changes current working dir");
                Terminal.WriteLine("    default - Changes to home dir");
                Terminal.WriteLine("    dir - change to named directory");
                Terminal.WriteLine("          dir is full path or partial path from current working directory");
                Terminal.WriteLine("    .. - change to parent directory");
            }
        }

        private Directory NavigateTo(string path)
        {
            return cwd;
        }

        #endregion
    }
}
