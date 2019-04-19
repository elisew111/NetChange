using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NetwProg
{
    public class Connection
    {
        public StreamReader Read;
        public StreamWriter Write;

        // Connection heeft 2 constructoren: deze constructor wordt gebruikt als wij CLIENT worden bij een andere SERVER
        public Connection(int port)
        {
            TcpClient client = new TcpClient("localhost", port);
            Read = new StreamReader(client.GetStream());
            Write = new StreamWriter(client.GetStream());
            Write.AutoFlush = true;

            // De server kan niet zien van welke poort wij client zijn, dit moeten we apart laten weten
            Write.WriteLine("Poort: " + Program.MijnPoort);

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
        }

        // Deze constructor wordt gebruikt als wij SERVER zijn en een CLIENT maakt met ons verbinding
        public Connection(StreamReader read, StreamWriter write)
        {
            Read = read; Write = write;

            // Start het reader-loopje
            new Thread(ReaderThread).Start();
        }

        // LET OP: Nadat er verbinding is gelegd, kun je vergeten wie er client/server is (en dat kun je aan het Connection-object dus ook niet zien!)

        // Deze loop leest wat er binnenkomt en print dit
        public void ReaderThread()
        {
                while (true)
                {
                    string input = Read.ReadLine();
                    string type = input.Split(' ')[0];
                    if (type == "Print") //print
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 2);
                        Console.WriteLine(delen[1]);

                    }
                    if (type == "Buur") //nieuwe buur
                    {
                        int poort = int.Parse(input.Split()[1]);

                        Program.EditPath(poort, poort.ToString(), 1);
                        Program.StuurBuren(Program.Buren[poort]);

                    }
                    if (type == "Delete") //delete buurt
                    {
                        int poort = int.Parse(input.Split()[1]);
                        Program.Delete(poort);
                    }
                    if (type == "Forward")
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 5);
                        int dest = int.Parse(delen[1]);
                        int _length = int.Parse(delen[2]);
                        string _closest = delen[3];
                        int poort = int.Parse(delen[4]);

                        

                        lock (Program.lockobj)
                        {
                        if (_length <= 20)
                        {
                            if (!Program.RoutingTables.ContainsKey(dest) && _length <= 20)
                            {
                                Dictionary<int, Path> table = new Dictionary<int, Path>();
                                Program.RoutingTables.Add(dest, table);
                            }

                            Path pad = new Path() { closest = _closest, length = _length };
                            Program.RoutingTables[dest][poort] = pad;
                        }
                        else
                        {
                            if(Program.RoutingTables.ContainsKey(dest))
                            {
                                Program.RoutingTables[dest].Remove(poort);
                            }
                        }
                        }

                        Program.EditPath(dest, poort.ToString(), (_length + 1));
                    }
                    if (type == "Doorsturen")
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 4);
                        int poort = int.Parse(delen[1]);
                        string bericht = delen[3];
                        Program.Bericht(poort, bericht);
                    }
                    if (type == "GetPath")
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 3);
                        int poort = int.Parse(delen[1]);
                        int dest = int.Parse(delen[2]);
                        lock (Program.lockobj)
                        {
                            if(Program.Paden.ContainsKey(dest))
                            {
                                Program.Buren[poort].Write.WriteLine("Forward " + dest + " " + Program.Paden[dest].length + " " + Program.Paden[dest].closest + " " + Program.MijnPoort.ToString());
                            }
                        }
                    }
                    if (type == "ForwardDelete")
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 3);
                        int dest = int.Parse(delen[1]);
                        string closest = delen[2].ToString();
                        Program.ForwardDelete(dest, closest);
                    }
                    if(type == "DeletePath")
                    {
                    Program.Paden.Remove(int.Parse(input.Split(' ')[1]));
                    }
                }
            
            
        }
    }
}