using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Runtime.CompilerServices;

namespace TcpClientAddOn
{
    public class User
    {
        string username;
        public TcpClient client;
        public User(string username, TcpClient client)
        {
            this.username = username;
            this.client = client;
        }
        public string getUsername()
        {
            return username;
        }
    }
}
