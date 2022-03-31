using System;

namespace SimpleShell
{
    public interface Shell
    {
        void Run(Terminal terminal);
    }

    public interface ShellFactory
    {
        Shell CreateShell(string name, Session session);
    }
}
