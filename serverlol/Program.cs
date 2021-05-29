using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TcpClientAddOn;

namespace Messenger
{

    class Program
    {


        static TcpListener listener;
        static TcpClient admin = null;
        static TcpClient screenShare = null;
        static List<TcpClient> clients = new List<TcpClient>();
        static List<User> userList = new List<User>();
        static List<Thread> mainThreads = new List<Thread>();
        static List<Thread> recieveThreadList = new List<Thread>();
        static bool listen = true;
        static Stopwatch stopWatch = new Stopwatch();
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            Thread checker = new Thread(() => listCheck());
            checker.Start();
            Thread listenTask = new Thread(() => Initialize());
            listenTask.Start();
            mainThreads.Add(checker);
            mainThreads.Add(listenTask);


        }
        static void Initialize()
        {

            string externalip = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
            externalip = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalip)[0].ToString();
            IPAddress hostIP = IPAddress.Parse(externalip);
            listener = new TcpListener(IPAddress.Any, 22581);
            listener.Start();
            Console.WriteLine("Server Started at: " + externalip);
            listen = true;
            int i = 0;
            while (listen)
            {
                Console.WriteLine("\rIteration: " + getTime());
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("new Client " + client.Client.RemoteEndPoint.ToString());
                clients.Add(client);
                IPAddress clientIP = IPAddress.Parse(client.Client.RemoteEndPoint.ToString().Split(':')[0]);

                if (clientIP.Equals(hostIP) && admin is null)
                {
                    Console.WriteLine("New Admin is " + client.Client.RemoteEndPoint.ToString());
                    admin = client;
                }
                Thread recieveTask = new Thread(() => recieve(client.GetStream(), client));
                recieveThreadList.Add(recieveTask);
                recieveTask.Start();
                i++;
                Console.WriteLine("initial:" + i);
                Console.WriteLine("Done");
            }
            Console.WriteLine("lol breaked out");
        }
        static void udpListen()
        {
            //https://stackoverflow.com/questions/4844581/how-do-i-make-a-udp-server-in-c
            byte[] data = new byte[1024000];
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 22581);
            UdpClient clientUDP = new UdpClient(ip);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            data = clientUDP.Receive(ref sender);

        }
        static void Write(Packet p)
        {
            Console.WriteLine("Write Called");
            foreach (TcpClient client in clients)
            {
                NetworkStream stream = client.GetStream();
                ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, p, ProtoBuf.PrefixStyle.Base128);
                stopWatch.Stop();
                Console.WriteLine("ELAPSED TIME FOR IMAGE: " + stopWatch.ElapsedMilliseconds);
                stopWatch.Reset();
            }
        }
        static void singleWrite(Packet p, TcpClient client)
        {
            Console.WriteLine("Single Write called, from: " + client.Client.RemoteEndPoint + " message is " + p.message);
            NetworkStream stream = client.GetStream();
            ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, p,ProtoBuf.PrefixStyle.Base128);

        }
        static void recieve(NetworkStream stream, TcpClient client)
        {
            try
            {
                Console.WriteLine("recieve called");
                byte[] data = new byte[24000000];
                Console.WriteLine("Formatter Init");
                while (true)
                {
                    try
                    {
                        Packet p = ProtoBuf.Serializer.DeserializeWithLengthPrefix<Packet>(stream, ProtoBuf.PrefixStyle.Base128);
                        if (p.messageType is messageType.IMAGE)
                        {
                            stopWatch.Start();
                            Console.WriteLine("\rImage sent");
                            Write(p);
                        }
                        else
                        {
                            Console.WriteLine(p.message);
                        }
                        if (p.messageType is messageType.STRING)
                        {
                            if (client == admin)
                            {
                                if (p.message.Contains("-kick"))
                                {
                                    string user = p.message.Split(' ')[1];
                                    IPAddress userIP = IPAddress.Parse(user);
                                    TcpClient kick = new TcpClient();
                                    foreach (TcpClient c in clients)
                                    {
                                        if (c.Client.RemoteEndPoint.ToString().Split(':')[0].Equals(userIP))
                                        {
                                            kick = c;
                                        }
                                    }
                                    Kick(kick);
                                }
                                else if (p.message.Contains("-getClient"))
                                {
                                    string a = messageType.STRING.ToString() + "~" + ConsoleColor.White + "~";
                                    foreach (TcpClient ccp in clients)
                                    {
                                        a += " " + ccp.Client.RemoteEndPoint.ToString();
                                    }
                                    singleWrite(p, client);
                                }
                                else
                                {
                                    Write(p);
                                }
                            }
                            else if (p.message.Contains("-getClient"))
                            {
                                string a = " ";
                                int i = 1;
                                foreach (TcpClient ccp in clients)
                                {
                                    a += " " + i + ": " + ccp.Client.RemoteEndPoint.ToString() + "\n";
                                    i++;
                                }
                                singleWrite(p, client);
                            }
                            else
                            {
                                Write(p);
                            }
                        }
                        p = null;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e);
            }
        }
        static void Kick(TcpClient client)
        {
            clients.Remove(client);

            client.Close();
        }
        static async void listCheck()
        {
            bool clientDisconnected = true;
            Console.WriteLine("Checker Initiated");
            List<TcpClient> removeList = new List<TcpClient>();
            Thread.Sleep(1000);
            while (clientDisconnected)
            {
                int r = 0;
                Console.Write("\r" + getTime() + " checking clients: " + r / 100 + " Connected Clients: " + clients.Count);
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].Connected)
                    {

                    }
                    else
                    {
                        clientDisconnected = false;
                        Console.WriteLine("CLIENT REMOVED: " + clients[i].Client.RemoteEndPoint);
                        clients.RemoveAt(i);

                    }
                }
                r++;
            }
            clientDisconnected = true;
            listCheck();

        }
        static string getTime()
        {
            string time = "[" + DateTime.Now.ToString("hh:mm:ss") + "]: ";
            return time;
        }
        static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("HELLO");
            Thread.Sleep(3000);
            foreach(TcpClient c in clients)
            {
                c.Close();
                c.Dispose();

            }
        }
    }
}
public enum messageType
{
    STRING,
    FILE,
    VIDEO,
    IMAGE,
    COMMAND
}
