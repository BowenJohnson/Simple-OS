using System;
using System.Collections.Generic;

namespace SimpleShell
{
    public interface SecuritySystem
    {
        int AddUser(string username);
        int UserID(string username);
        bool NeedsPassword(string username);
        void SetPassword(string username, string password);
        int Authenticate(string username, string password);
        string UserName(int userID);
        string UserHomeDirectory(int userID);
        string UserPreferredShell(int userID);
    }
}
