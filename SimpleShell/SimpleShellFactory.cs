using System;

namespace SimpleShell
{
    class SimpleShellFactory : ShellFactory
    {
        public Shell CreateShell(string name, Session session)
        {
            if (name == "pshell")
                return new SimpleShell(session);

            return null;
        }
    }
}
