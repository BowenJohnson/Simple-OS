using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleShell
{
    class DotNetConsoleTerminal : TerminalDriver
    {
        private TerminalInterruptHandler handler;
        private Queue<char> partialLineQueue;
        private KeyMonitor monitor;

        public DotNetConsoleTerminal()
        {
            handler = null;
            monitor = null;
            partialLineQueue = null;
        }

        public void Connect()
        {
            // NOTE: blocks until connected
            if (monitor == null)
            {
                partialLineQueue = new Queue<char>();
                monitor = new KeyMonitor();
                monitor.KeyPressed += Monitor_KeyPressed;
            }
        }

        public void Disconnect()
        {
            // NOTE: blocks until disconnected
            if (monitor != null)
            {
                partialLineQueue = null;
                monitor.KeyPressed -= Monitor_KeyPressed;
                monitor = null;
            }
        }

        public char RecvChar()
        {
            // NOTE: non-blocking, just returns last character received
            return partialLineQueue.Count > 0 ? partialLineQueue.Dequeue() : ' ';
        }

        public void SendChar(char c)
        {
            Console.Write(c);
        }

        public void SendNewLine()
        {
            Console.WriteLine();
        }

        public void InstallInterruptHandler(TerminalInterruptHandler handler)
        {
            this.handler = handler;
        }

        private void Monitor_KeyPressed(object sender, ConsoleKeyInfo e)
        {
            switch (e.Key)
            {
                case ConsoleKey.Enter:
                    handler?.HandleInterrupt(TerminalInterrupt.ENTER);
                    break;

                case ConsoleKey.Backspace:
                    handler?.HandleInterrupt(TerminalInterrupt.BACK);
                    break;

                case ConsoleKey.F1:
                    handler?.HandleInterrupt(TerminalInterrupt.CONNECT);
                    break;

                default:
                    partialLineQueue.Enqueue(e.KeyChar);
                    handler?.HandleInterrupt(TerminalInterrupt.CHAR);
                    break;
            }
        }

        private class KeyMonitor
        {
            private Thread theThread;

            public KeyMonitor()
            {
                theThread = new Thread(ThreadProc);
                theThread.IsBackground = true;
                theThread.Start(this);
            }
            
            public event EventHandler<ConsoleKeyInfo> KeyPressed;

            private void ThreadProc(object param)
            {
                while (true)
                {
                    // prevent echoing character automatically
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    KeyPressed?.Invoke(this, key);
                }
            }
        }
    }
}
