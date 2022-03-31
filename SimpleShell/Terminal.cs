// Bowen Johnson
// Spring 2021

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace SimpleShell
{
    public class Terminal
    {
        // NOTE: performs line discipline over driver
        private TerminalDriver driver;
        private LineQueue completedLineQueue;
        private Handler handler;

        public Terminal(TerminalDriver driver)
        {
            completedLineQueue = new LineQueue();
            handler = new Handler(driver, completedLineQueue);

            this.driver = driver;
            this.driver.InstallInterruptHandler(handler);
        }

        public void Connect()
        {
            driver.Connect();
        }

        public void Disconnect()
        {
            driver.Disconnect();
        }

        public bool Echo { get { return handler.Echo; } set { handler.Echo = value; } }

        public string ReadLine()
        {
            // NOTE: blocks until a line of text is available
            
            return completedLineQueue.Remove();
        }

        public void Write(string line)
        {
           foreach (char c in line)
           {
                if (c == '\n' || c == '\r')
                {
                    driver.SendNewLine();
                }
                else
                {
                    driver.SendChar(c);
                }              
           }
        }

        public void WriteLine(string line)
        {
            // throws on a new line at the end
            Write(line);
            driver.SendNewLine();
        }

        private class LineQueue
        {
            private Queue<string> theQueue;
            private Mutex mutex;
            private ManualResetEvent hasItemsEvent;

            public LineQueue()
            {
                this.theQueue = new Queue<string>();
                this.mutex = new Mutex();
                this.hasItemsEvent = new ManualResetEvent(false);   // initially is empty
            }

            public void Insert(string s)
            {
                // wait until we have the mutex
                mutex.WaitOne();

                // insert into the buffer
                theQueue.Enqueue(s);

                // signal any threads waiting to remove an object
                hasItemsEvent.Set();

                // release that mutex back into the wilds
                mutex.ReleaseMutex();
            }

            public string Remove()
            {
                // wait until there is at least one object in the queue and we have the mutex
                WaitHandle.WaitAll(new WaitHandle[] { mutex, hasItemsEvent });

                // remove the item from the buffer
                string str = theQueue.Dequeue();

                // block any threads waiting to remove, if the queue is empty
                if (theQueue.Count == 0)
                {
                    hasItemsEvent.Reset();
                }
                
                // release that mutex back into the wilds
                mutex.ReleaseMutex();

                return str;
            }

            public int Count()
            {
                // wait until we have the mutex
                mutex.WaitOne();

                // return the number of items in the queue
                int count = theQueue.Count;

                // release that mutex back into the wilds
                mutex.ReleaseMutex();

                return count;
            }
        }

        class Handler : TerminalInterruptHandler
        {
            private TerminalDriver driver;
            private List<char> partialLineQueue;
            private LineQueue completedLineQueue;

            public Handler(TerminalDriver driver, LineQueue completedLineQueue)
            {
                this.driver = driver;
                this.completedLineQueue = completedLineQueue;
                this.partialLineQueue = new List<char>();
            }

            public bool Echo { get; set; }

            public void HandleInterrupt(TerminalInterrupt interrupt)
            {
                switch (interrupt)
                {
                    case TerminalInterrupt.CHAR:
                        // queue up the characters until we have a completed line
                        char ch = driver.RecvChar();
                        partialLineQueue.Add(ch);
                        if (Echo)
                        {
                            driver.SendChar(ch);
                        }

                        break;

                    case TerminalInterrupt.ENTER:
                        // get all the characters from the partial line queue and create a completed line
                        if (Echo)
                        {
                            driver.SendNewLine();
                        }

                        string line = new string(partialLineQueue.ToArray());
                        partialLineQueue.Clear();
                        completedLineQueue.Insert(line);


                        break;

                    case TerminalInterrupt.BACK:
                        // throw away the last character entered
                        if (partialLineQueue.Count > 0)
                        {
                            partialLineQueue.RemoveAt(partialLineQueue.Count - 1);

                            if (Echo)
                            {
                                driver.SendChar((char)0x08);
                                driver.SendChar(' ');
                                driver.SendChar((char)0x08);
                            }
                        }
                        else
                        {
                            driver.SendChar((char)0x07);
                        }

                        break;
                }
            }
        }
    }
}
