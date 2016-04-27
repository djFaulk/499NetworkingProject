using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace _499Client
{
    class Program
    {
        /* THINGS TO DO/THINK ABOUT
        
            Getting Files from other Clients
                we get the IP address and port when we call get file
                We need to intercept the client input and not send it
                call a function where a new socket is created, requests file
                Clients need a function to accept a new connection and send data
                Clients need to have a list of sockets connected to them
                Clients may need two thread: one to communicate with server, one to communicate with clients

            dont use ports under 1024

            
        */


        private static List<Socket> otherClients;

        private static Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static int listenPort;
        private static Socket clientConnectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Socket serverConnectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static byte[] buffr;
        private const int buffrSize = 1024;
        private static List<string> fileList;

        static void Main(string[] args)
        {
            Console.Title = "Client";
            InitClient();
            Console.ReadLine();
            Exit();
        }

        private static void InitClient()
        {
            //Initialize Thread For listening
            Thread clientConnectThread = new Thread(new ThreadStart(initListen));
            clientConnectThread.Start();

            //Initialize Client Data
            //Connect to Server
            bool correctIPformat = false;
            string[] IParray = null;
            while(!correctIPformat)
            {
                Console.Clear();
                Console.WriteLine("Please enter the local IP and port of the machine you wish to connect to, seperated by a ':' ");
                string IPstring = Console.ReadLine();
                IParray = FormatText(IPstring);
                if (IParray.Length != 2)
                {
                    Console.WriteLine("Incorrect Format");
                    correctIPformat = false;
                }
                else
                {
                    correctIPformat = true;
                }
            }
            
            IPAddress serverIP;
            int serverPort;
            try
            {
                //IPstring = "192.168.0.2";
                serverIP = IPAddress.Parse(IParray[0]);
                serverPort = Int32.Parse(IParray[1]);
                Console.WriteLine("Attempting to Connect to IP {0} on Port {1}", serverIP, serverPort);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }

            //Two threads: One to connect to server and send data, one to wait for a connection
            //Initialize Global variables
            otherClients = new List<Socket>();
            buffr = new byte[buffrSize];
            fileList = new List<string>();
            //DirectoryInfo dinfo = new DirectoryInfo(Environment.CurrentDirectory.ToString());
            string path = Environment.CurrentDirectory.ToString();
            fileList.AddRange(Directory.GetFiles(path, "*.txt"));

            //Thread To Connect to Server
            //Thread servConnectThread = new Thread(() =>LoopConnect(serverIP, serverPort));
            //servConnectThread.Start();
            //Thread to Listen for other Clients
            
            LoopConnect(serverIP, serverPort);
            //initListen();
            //SendLoop(); Called from LoopConnect upong connection to server
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            serverConnectSocket.Shutdown(SocketShutdown.Both);
            serverConnectSocket.Close();
            Environment.Exit(0);
        }

        private static void SendLoop()
        {
            Console.WriteLine("Type 'exit' to leave.");
            Console.WriteLine("The Local IP is " + getLocalIP().ToString());

            while (true)
            {
                //Send Request
                Console.Write("Enter a Request: ");
                string req = Console.ReadLine();
                string[] words = FormatText(req);
                //Check to see if Words is more than 2. If it is, wrong format

                if (words[0] == "exit")
                {
                    SendString("exit");
                    RecvData();
                    Exit();
                }
                else if (words[0] == "get ip")
                {
                    Console.WriteLine("The Local IP is " + getLocalIP().ToString());
                }
                else if (words[0] == "add")
                {
                    string path = Environment.CurrentDirectory.ToString();
                    Console.WriteLine("Current Directory is: " + path);

                    path = path + @"\" + words[1];
                    //Console.WriteLine("File Path is: " + path);
                    bool exists = File.Exists(path) ? true : false;
                    Console.WriteLine(File.Exists(path) ? "File Exists" : "File Does Not Exist");
                    if (exists)
                    {
                        req = string.Join(":", words[0], words[1]);
                        Console.WriteLine("data to be sent looks like: " + req);
                        SendString(req);
                        RecvData();
                    }

                }
                else if (words[0] == "get")
                {
                    SendString(req);
                    Thread getFileThread = new Thread(new ThreadStart(RecvIP));
                    getFileThread.Start();
                }
                else if (words[0] == "connect")
                {
                    //This client is trying to connect to another client
                    //We need to create a new socket, and attempt to connect to the other client
                    //THe clients already have somehting to listen for clients
                    //And the client already tries to connect
                    //Mash these two together

                    //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                }
                else
                {
                    req = string.Join("", words);
                    Console.WriteLine("data to be sent looks like: " + req);
                    SendString(req);
                    RecvData();
                }

           
            }

        }
        private static void connectClient(string ipAddress)
        {
            Socket socket;
            IPAddress clientConnectTo = IPAddress.Parse(ipAddress);
        }
        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            serverConnectSocket.Send(buffer, 0, buffer.Length, SocketFlags.None); //exception  Specified argument was out of the range of valid values.
        }

        private static void RecvData()
        {
            //Recieve Data
            byte[] recbuffer = new byte[1024];
            int rec = serverConnectSocket.Receive(recbuffer, SocketFlags.None);
            if (rec == 0) return;
            byte[] data = new byte[rec];
            Array.Copy(recbuffer, data, rec);
            string thing = Encoding.ASCII.GetString(data);
            Console.WriteLine("Recieved: " + thing);
        }

        private static void RecvIP()
        {
            //Recieving IP. Attempt to Connect
            byte[] recbuffer = new byte[1024];
            int rec = serverConnectSocket.Receive(recbuffer, SocketFlags.None);
            if (rec == 0) return;
            byte[] data = new byte[rec];
            Array.Copy(recbuffer, data, rec);
            string thing = Encoding.ASCII.GetString(data);
            Console.WriteLine(thing);
            Console.WriteLine("Do you want to Retrieve this file? Y/N");
            string answer = Console.ReadLine();
            if(answer.ToLower() == "y" || answer.ToLower() == "yes")
            {
                string[] words = FormatText(thing);
                FileConnect(IPAddress.Parse(words[1]), Int32.Parse(words[2]));
            }

        }


        private static void LoopConnect(IPAddress serverIP, int port)
        {
            int attempts = 0;

            while (!serverConnectSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection Attempts: " + attempts);
                    serverConnectSocket.Connect(serverIP, port);
                }
                catch (SocketException s)
                {
                    Console.Clear();
                }

            }

            Console.Clear();
            Console.WriteLine("Connected");
            byte[] data = new byte[buffrSize];
            IPEndPoint ipEnd = listenSocket.LocalEndPoint as IPEndPoint;
            data = Encoding.ASCII.GetBytes(ipEnd.Port.ToString());
            serverConnectSocket.Send(data);
            Console.WriteLine("Client is listening on port:" + ipEnd.Port);
            SendLoop();
        }

        private static void FileConnect(IPAddress serverIP, int port)
        {
            int attempts = 0;

            while (!serverConnectSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection Attempts: " + attempts);
                    serverConnectSocket.Connect(serverIP, port);
                }
                catch (SocketException s)
                {
                    Console.Clear();
                }

            }

            Console.Clear();
            Console.WriteLine("Connected");
            //Send text of file to get
            //get file
            //save file
            //end
        }

        private static string[] FormatText(string text)
        {
            text = text.ToLower();
            //Console.WriteLine("Text Before remove white space: " + text);
            text = string.Join("", text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            //Console.WriteLine("Text after remove White Space: " + text);

            char[] delimiterChars = { ':' };
            string[] words = text.Split(delimiterChars);

            return words;
        }

        private static IPAddress getLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            if(!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Console.WriteLine("Network Unavailable");
                return null;
            }

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            throw new Exception("Local IP Address Not Found");
        }

        private static void initListen()
        {
            Console.WriteLine("Intializing Listen on Client...");
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = getLocalIP();
            Random rand = new Random();
            listenPort = rand.Next(1024,60000);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, listenPort);
            Console.WriteLine("Listener Port is: " + localEndPoint.Port.ToString());

            try
            {
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(1);
                //listenSocket.BeginAccept(AcceptCallback, null);
                Console.WriteLine("Client Listen Initialized.");
                ListenLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ListenLoop()
        {
            while (true)
            {
                Socket handler;
                try
                {
                    handler = listenSocket.Accept();
                    otherClients.Add(handler);
                    Thread clientServe = new Thread(() => ServeLoop(handler));
                    clientServe.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return;
                }

                Console.WriteLine("Client has connected.");
            }

        }

        private static void ServeLoop(Socket client)
        {
            while (true)
            {
                int recv;
                byte[] recvBuff = new byte[buffrSize];
                try
                {
                    recv = client.Receive(recvBuff, SocketFlags.None);
                }
                catch (SocketException)
                {
                    Console.Write("Client has disconnected.");
                    client.Close();
                    otherClients.Remove(client);
                    return;
                }

                string text = Encoding.ASCII.GetString(recvBuff, 0, recv);
                Console.WriteLine("Recieved: " + text);

                //will recieve text of file to send
                //send file
                //end
            }


        }

        private static void AcceptCallback(IAsyncResult IAR)
        {
            Socket clientSocket;

            try
            {
                clientSocket = listenSocket.EndAccept(IAR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            otherClients.Add(clientSocket);
            Console.WriteLine("Client has Connected...");
            clientSocket.BeginReceive(buffr, 0, buffrSize, SocketFlags.None, RecvCallBack, clientSocket);
            listenSocket.BeginAccept(AcceptCallback, null);

        }

        private static void RecvCallBack(IAsyncResult IAR)
        {
            Socket socket = (Socket)IAR.AsyncState;
            int recv;

            try
            {
                recv = socket.EndReceive(IAR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client Disconnected.");
                socket.Close();
                otherClients.Remove(socket);
                return;
            }

            byte[] tempBuff = new byte[recv];
            Array.Copy(buffr, tempBuff, recv);

            string text = Encoding.ASCII.GetString(tempBuff);
            Console.WriteLine("Text Recieved: " + text);

            char[] delimiterChars = { ':' };
            string[] words = text.Split(delimiterChars);

            byte[] data;
            string input = words[0].ToLower();

            switch(input)
            {
                case "get":
                    string path = Environment.CurrentDirectory.ToString();
                    Console.WriteLine("Current Directory is: " + path);

                    path = path + @"\" + words[1];
                    Console.WriteLine("File Path is: " + path);
                    Console.WriteLine(File.Exists(path) ? "File Exists" : "File Does Not Exist");
                    break;
            }

            try
            {
                socket.BeginReceive(buffr, 0, buffrSize, SocketFlags.None, RecvCallBack, socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
