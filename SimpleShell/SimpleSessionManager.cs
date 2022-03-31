using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleFileSystem;

namespace SimpleShell
{
    public class SimpleSessionManager : SessionManager
    {
        private class SimpleSession : Session
        {
            private int userID;
            private SecuritySystem security;
            private FileSystem filesystem;
            private ShellFactory shells;
            private Directory homeDir;
            private Shell shell;
            private Terminal terminal;

            public SimpleSession(SecuritySystem security, FileSystem filesystem, ShellFactory shells, Terminal terminal, int userID)
            {
                this.security = security;
                this.filesystem = filesystem;
                this.shells = shells;
                this.terminal = terminal;
                this.userID = userID;

                // get user's home directory
                homeDir = filesystem.Find(security.UserHomeDirectory(UserID)) as Directory;

                // identify user's shell
                shell = shells.CreateShell(security.UserPreferredShell(UserID), this);
            }

            public int UserID => userID;
            public string Username => security.UserName(userID);
            public Terminal Terminal => terminal;
            public Shell Shell => shell;
            public Directory HomeDirectory => homeDir;
            public FileSystem FileSystem => filesystem;
            public SecuritySystem SecuritySystem => security;

            public void Run()
            {
                shell.Run(terminal);
            }

            public void Logout()
            {
                // empty resources for next user
                userID = -1;
                security = null;
                filesystem = null;
                shells = null;
                homeDir = null;
                shell = null;
                terminal = null;
            }
        }

        private SecuritySystem security;
        private FileSystem filesystem;
        private ShellFactory shells;

        public SimpleSessionManager(SecuritySystem security, FileSystem filesystem, ShellFactory shells)
        {
            this.security = security;
            this.filesystem = filesystem;
            this.shells = shells;
        }

        public Session NewSession(Terminal terminal)
        {
            // ask the user to login
            // give them 3 tries
            int tries = 3;
            int userID = -42;
            while (userID < 0 && tries > 0)
            {
                try
                {
                    // prompt for user name
                    terminal.Write("Username: ");
                    terminal.Echo = true;
                    string userName = terminal.ReadLine();

                    // determine if the user needs to set their password
                    if (security.NeedsPassword(userName))
                    {
                        // prompt for new password
                        terminal.Write("Type in new password: ");
                        terminal.Echo = true;
                        string newPW = terminal.ReadLine();
                        security.SetPassword(userName, newPW);

                        // return them to the login prompt
                        continue;
                    }

                    // prompt for password
                    terminal.Write("Password: ");
                    terminal.Echo = false;
                    string password = terminal.ReadLine();
                    terminal.WriteLine("");
                    terminal.Echo = true;

                    // authenticate user
                    userID = security.Authenticate(userName, password);
                }
                catch (Exception)
                {
                    // wrong pw
                    terminal.WriteLine("Invalid Username or Password!");
                    tries--;
                }
            }

            if (userID > 0)
            {
                // create a new session and return it
                return new SimpleSession(security, filesystem, shells, terminal, userID);
            }
            else
            {
                // user failed authentication too many times
                terminal.WriteLine("Too many incorrect attempts! Good day.");
                return null;
            }
        }
    }
}
