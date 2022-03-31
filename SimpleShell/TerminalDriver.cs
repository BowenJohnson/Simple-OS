using System;


namespace SimpleShell
{
    public enum TerminalInterrupt { CONNECT, CHAR, ENTER, BACK }

    public interface TerminalInterruptHandler
    {
        void HandleInterrupt(TerminalInterrupt interrupt);
    }

    public interface TerminalDriver
    {
        void Connect();
        void Disconnect();

        char RecvChar();

        void SendChar(char c);
        void SendNewLine();

        void InstallInterruptHandler(TerminalInterruptHandler handler);
    }
}
