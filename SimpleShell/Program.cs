using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SimpleFileSystem;
using System.Text;

namespace SimpleShell
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestTerminalDriver();
            //TestTerminal();
            //TestSecuritySystem();
            TestSessionManager();
        }

        #region terminal driver

        static void TestTerminalDriver()
        {
            TerminalDriver driver = new DotNetConsoleTerminal();
            driver.InstallInterruptHandler(new TestHandler(driver));
            driver.Connect();

            driver.SendChar('f');
            driver.SendChar('o');
            driver.SendChar('o');
            driver.SendNewLine();

            Thread.Sleep(100000);

            driver.Disconnect();
        }

        class TestHandler : TerminalInterruptHandler
        {
            private TerminalDriver driver;

            public TestHandler(TerminalDriver driver)
            {
                this.driver = driver;
            }

            public void HandleInterrupt(TerminalInterrupt interrupt)
            {
                switch (interrupt)
                {
                    case TerminalInterrupt.CHAR:
                        Trace.WriteLine("Received character: " + driver.RecvChar());
                        break;

                    case TerminalInterrupt.ENTER:
                        Trace.WriteLine("Received ENTER");
                        break;

                    case TerminalInterrupt.BACK:
                        Trace.WriteLine("Received BACK");
                        break;

                    case TerminalInterrupt.CONNECT:
                        Trace.WriteLine("Received CONNECT");
                        break;
                }
            }
        }

        #endregion

        #region terminal

        static void TestTerminal()
        {
            TerminalDriver driver = new DotNetConsoleTerminal();
            Terminal term = new Terminal(driver);
            term.Connect();

            term.Write("Enter some text: ");
            term.Echo = true;
            string s1 = term.ReadLine();
            term.WriteLine("You entered: " + s1);

            term.Write("Enter your password: ");
            term.Echo = false;
            string pwd = term.ReadLine();
            term.WriteLine("");
            term.WriteLine("Got your password!");

            term.Disconnect();
        }

        #endregion

        #region security system

        static void TestSecuritySystem()
        {
            SecuritySystem security = new SimpleSecurity();
            security.AddUser("pete");
            if (security.NeedsPassword("pete"))
            {
                try
                {
                    security.SetPassword("pete", "foobar");
                }
                catch(Exception ex)
                {

                }

                security.SetPassword("pete", "foobarfoobarfoobar");
            }
            int userID = security.Authenticate("pete", "foobarfoobarfoobar");
            Console.WriteLine("UserID " + userID.ToString());
            Console.WriteLine("Username " + security.UserName(userID));
            Console.WriteLine("Home Directory " + security.UserHomeDirectory(userID));
            Console.WriteLine("Shell " + security.UserPreferredShell(userID));
        }

        #endregion

        #region session manager

        static void TestSessionManager()
        {
            // disk
            //VolatileDisk disk = new VolatileDisk(1);
            PersistentDisk disk = new PersistentDisk(1, "disk1");
            
            // create and write our OS to disk if needed
            CreateOSOnDisk(disk);

            // set up file system and mount disk
            disk.TurnOn();
            FileSystem filesystem = new SimpleFS();
            filesystem.Mount(disk, "/");
            
            // security system
            SecuritySystem security = new SimpleSecurity(filesystem, "/passwd");

            // add pete and his test data if needed
            CreatePete(security, filesystem);
            
            // session manager
            ShellFactory shells = new SimpleShellFactory();
            SessionManager sessionmanager = new SimpleSessionManager(security, filesystem, shells);

            // terminal
            TerminalDriver driver = new DotNetConsoleTerminal();
            Terminal term = new Terminal(driver);
            term.Connect();

            // allow terminals to connect and establish new sessions for users
            try
            {
                while (true)
                {
                    Session session = sessionmanager.NewSession(term);
                    if (session == null)
                        throw new Exception("Failed to create new session!");

                    session.Run();

                    // after session exits, time to log out!
                    session.Logout();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            term.Disconnect();
            disk.TurnOff();
        }

        static void CreateOSOnDisk(DiskDriver disk)
        {
            // create the operating system on disk if not present

            disk.TurnOn();

            // try to mount the disk, format if needed first
            FileSystem filesystem = new SimpleFS();
            try
            {
                filesystem.Mount(disk, "/");
            }
            catch (Exception)
            {
                filesystem.Format(disk);
                filesystem.Mount(disk, "/");
            }

            Directory rootDir = filesystem.GetRootDirectory();

            // password file for security subsystem if not present
            if (filesystem.Find("/passwd") == null)
                rootDir.CreateFile("passwd");

            // user home directory and files if not present
            if (filesystem.Find("/users") == null)
                rootDir.CreateDirectory("users");

            // root user if not present
            SecuritySystem security = new SimpleSecurity(filesystem, "/passwd");
            try
            {
                security.AddUser("root");
            }
            catch (Exception)
            {
            }

            disk.TurnOff();
        }

        static void CreatePete(SecuritySystem security, FileSystem filesystem)
        {
            // add user pete if not already there
            int peteUserID = -1;
            try
            {
                peteUserID = security.UserID("pete");
            }
            catch (Exception)
            {
                peteUserID = security.AddUser("pete");
                security.SetPassword("pete", "foobarfoobarfoobar");
            }

            // create some test files for user pete if needed
            if (filesystem.Find("/users/pete/subdir") == null)
            {
                Directory peteDir = (Directory)filesystem.Find(security.UserHomeDirectory(peteUserID));
                Directory subDir = peteDir.CreateDirectory("subdir");
                File file1 = subDir.CreateFile("file1");
                File file2 = subDir.CreateFile("file2");
                FileStream stream1 = file1.Open();
                stream1.Write(0, ASCIIEncoding.ASCII.GetBytes("hello from file 1!"));
                stream1.Close();
                FileStream stream2 = file2.Open();
                stream2.Write(0, ASCIIEncoding.ASCII.GetBytes("back at you from file 2!"));
                stream2.Close();
            }
        }

        #endregion

    }
}
