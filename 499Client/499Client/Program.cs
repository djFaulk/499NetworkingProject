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

            
        */


        private static List<Socket> otherClients;
        private static Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Socket servSendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            Console.WriteLine("Please enter the local IP of the machine you wish to connect to. Press return to loopback: ");
            string IPstring = Console.ReadLine();
            IPAddress serverIP;
            try
            {
                //IPstring = "192.168.0.2";
                serverIP = IPAddress.Parse(IPstring);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                serverIP = IPAddress.Loopback;
            }

            //Two threads: One to connect to server and send data, one to wait for a connection
            //Init List object
            otherClients = new List<Socket>();
            buffr = new byte[buffrSize];
            fileList = new List<string>();

            //Thread To Connect to Server
            //Thread servConnectThread = new Thread(() =>LoopConnect(serverIP));
            
            //Thread to Listen for other Clients
            Thread clientConnectThread = new Thread(new ThreadStart(initListen));
            LoopConnect(serverIP, 100);
            //SendLoop(); Called from LoopConnect upong connection to server
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            servSendSocket.Shutdown(SocketShutdown.Both);
            servSendSocket.Close();
            Environment.Exit(0);
        }

        private static void SendLoop()
        {
            Console.WriteLine("Type 'exit' to leave.");
            getLocalIP();

            while (true)
            {
                //Send Request
                Console.Write("Enter a Request: ");
                string req = Console.ReadLine();
                string[] words = FormatText(req);
                //Check to see if Words is more than 2. If it is, wrong format

                if (words[0] == "exit")
                {
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
                    Console.WriteLine("File Path is: " + path);
                    Console.WriteLine(File.Exists(path) ? "File Exists" : "File Does Not Exist");

                    req = string.Join(":", words[0], words[1]);
                    Console.WriteLine("data to be sent looks like: " + req);
                    SendString(req);
                    RecvData();

                }
                else if (words[0] == "get")
                {
                    SendString(req);
                    RecvIP();
                }
                else if (words[0] == "connect")
                {
                    //This client is trying to connect to another client
                    //We need to create a new socket, and attempt to connect to the other client
                    //THe clients already have somehting to listen for clients
                    //And the client already tries to connect
                    //Mash these two together

                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                }
                else
                {
                    Console.WriteLine("data to be sent looks like: " + req);
                    SendString(req);
                    Thread getFile = new Thread(new ThreadStart (RecvData));


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
            servSendSocket.Send(buffer, 0, buffer.Length, SocketFlags.None); //exception  Specified argument was out of the range of valid values.
        }

        private static void RecvData()
        {
            //Recieve Data
            byte[] recbuffer = new byte[1024];
            int rec = servSendSocket.Receive(recbuffer, SocketFlags.None);
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
            int rec = servSendSocket.Receive(recbuffer, SocketFlags.None);
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

            while (!servSendSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection Attempts: " + attempts);
                    servSendSocket.Connect(serverIP, port);
                }
                catch (SocketException s)
                {
                    Console.Clear();
                }

            }

            Console.Clear();
            Console.WriteLine("Connected");
            SendLoop();
        }

        private static void FileConnect(IPAddress serverIP, int port)
        {
            int attempts = 0;

            while (!servSendSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection Attempts: " + attempts);
                    servSendSocket.Connect(serverIP, port);
                }
                catch (SocketException s)
                {
                    Console.Clear();
                }

            }

            Console.Clear();
            Console.WriteLine("Connected");
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
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 0);

            try
            {
                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(1);
                listenSocket.BeginAccept(AcceptCallback, null);
                Console.WriteLine("Client Listen Initialized.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
