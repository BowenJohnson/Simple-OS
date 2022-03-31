// Bowen Johnson
// Spring 2021

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SimpleFileSystem;
using System.Text.RegularExpressions;


namespace SimpleShell
{
    public class SimpleSecurity : SecuritySystem
    {
        private class User
        {
            public int userID;
            public string userName;
            public string password;
            public string homeDirectory;
            public string shell;
        }

        private int nextUserID;
        private Dictionary<int, User> usersById;        // userID -> User

        private FileSystem filesystem;
        private string passwordFileName;
        
        public SimpleSecurity()
        {
            nextUserID = 1;
            usersById = new Dictionary<int, User>();
        }

        public SimpleSecurity(FileSystem filesystem, string passwordFileName)
        {
            nextUserID = 1;
            usersById = new Dictionary<int, User>();
            this.filesystem = filesystem;
            this.passwordFileName = passwordFileName;

            LoadPasswordFile();
        }

        private void LoadPasswordFile()
        {
            // Read all users from the password file
            // userID;username;password;homedir;shell

            // open stream  on pw file
            File pwFile = filesystem.Find(passwordFileName) as File;
            FileStream fStream = pwFile.Open();

            // read pw file
            string pwFileText = ASCIIEncoding.UTF8.GetString(fStream.Read(0, pwFile.Length));

            // close the stream
            fStream.Close();

            if (!string.IsNullOrWhiteSpace(pwFileText))
            {
                // break up user data into parts and add them
                string[] lines = pwFileText.Split('\n');
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string[] parts = line.Split(';');

                        if (parts.Length == 5)
                        {
                            User user = new User();
                            user.userID = int.Parse(parts[0]);
                            user.userName = parts[1];
                            user.password = parts[2];
                            if (string.IsNullOrWhiteSpace(user.password))
                            {
                                user.password = null;
                            }
                            user.homeDirectory = parts[3];
                            user.shell = parts[4];
                            usersById.Add(user.userID, user);
                        }
                    }
                }

                // find largest used ID & set this.nextUserID to largest + 1
                nextUserID = usersById.Keys.Max() + 1;
            }
        }

        private void SavePasswordFile()
        {
            // Save all users to the password file
            // userID;username;password;homedir;shell

            string pwFileContent = "";

            foreach (User user in usersById.Values)
            {
                // add a line of text to the pw file
                pwFileContent += user.userID.ToString() + ";";
                pwFileContent += user.userName + ";";
                pwFileContent += user.password + ";";
                pwFileContent += user.homeDirectory + ";";
                pwFileContent += user.shell + "\n";
            }

            // open stream on pw file at byte 0
            File pwFile = filesystem.Find(passwordFileName) as File;
            FileStream fStream = pwFile.Open();

            // write contents
            fStream.Write(0, ASCIIEncoding.UTF8.GetBytes(pwFileContent));

            // close stream
            fStream.Close();
        }

        private User UserByName(string username)
        {
            return usersById.Values.FirstOrDefault(u => u.userName == username);
        }

        public int AddUser(string username)
        {
            // new user name check
            if (string.IsNullOrEmpty(username))
            {
                throw new Exception("Invalid username!");
            }
            if (UserByName(username) != null)
            {
                throw new Exception("Username already exists!");
            }

            // create a new user with default home directory and shell
            User user = new User();
            user.userID = nextUserID++;
            user.userName = username;

            // default home dir
            // default preffered shell
            user.homeDirectory = "/users/" + username;
            user.shell = "pshell";

            // initially empty password
            user.password = null;

            // add user to the dictionary
            usersById[user.userID] = user;

            // create user's home directory if needed
            if (filesystem.Find(user.homeDirectory) == null)
            {
                Directory userDir = filesystem.Find("/users") as Directory;
                userDir.CreateDirectory(username);
            }

            // save the user to the password file
            SavePasswordFile();

            // return user id
            return user.userID;
        }

        public int UserID(string username)
        {
            // lookup user by username and return user id
            // find user in dictionary
            User user = UserByName(username);
            if (user != null)
            {
                return user.userID;
            }

            throw new Exception("User not found!"); 
        }

        public bool NeedsPassword(string username)
        {
            // return true if user needs a password set
            // find user in dictionary
            User user = UserByName(username);
            if (user != null)
            {
                return user.password == null;
            }

            throw new Exception("User not found!");
        }

        public void SetPassword(string username, string password)
        {
            // set user's password
            // find user in dictionary
            User user = UserByName(username);
            if (user == null)
            {
                throw new Exception("User not found!");
            }

            // validate it meets any rules
            // password laws..
            //      length >= 15
            //      lower and uppercase letters
            Regex reg = new Regex("[a-zA-Z]");

            if (password.Length < 15 || !reg.IsMatch(password))
            {
               throw new Exception("Invalid password!");
            }

            // save password
            user.password = password;

            // save it to the password file
            SavePasswordFile();
        }

        public int Authenticate(string username, string password)
        {
            // authenticate user by username/password
            User user = UserByName(username);
            if (user == null)
            {
                throw new Exception("User not found!");
            }

            // check that pw
            if (user.password !=password)
            {
                throw new Exception("Authentication is a no go!");
            }

            // return user id
            return user.userID;
        }

        public string UserName(int userID)
        {
            // lookup user by user id and return username
            User user = LookupUserById(userID);
            if (user != null)
            {
                return user.userName;
            }

            return null;
        }

        public string UserHomeDirectory(int userID)
        {
            // lookup user by user id and return home directory
            User user = LookupUserById(userID);
            if (user != null)
            {
                return user.homeDirectory;
            }

            return null;
        }

        public string UserPreferredShell(int userID)
        {
            // lookup user by user id and return shell name
            User user = LookupUserById(userID);
            if (user != null)
            {
                return user.shell;
            }

            return null;
        }

        private User LookupUserById(int userID)
        {
            if (usersById.ContainsKey(userID))
            {
                return usersById[userID];
            }

            return null;
        }
    }
}
