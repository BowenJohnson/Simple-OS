using System;
using SimpleFileSystem;


namespace SimpleShell
{
    public interface Session
    {
        int UserID { get; }
        string Username { get; }
        Terminal Terminal { get; }
        Shell Shell { get; }
        Directory HomeDirectory { get; }
        FileSystem FileSystem { get; }
        SecuritySystem SecuritySystem { get; }

        void Run();
        void Logout();
    }

    public interface SessionManager
    {
        Session NewSession(Terminal terminal);
    }
}
