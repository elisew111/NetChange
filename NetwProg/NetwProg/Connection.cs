﻿using System;
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

            // Start het reader-loopje met eigen thread
            new Thread(ReaderThread).Start();
        }

        // LET OP: Nadat er verbinding is gelegd, kun je vergeten wie er client/server is (en dat kun je aan het Connection-object dus ook niet zien!)

        // Deze loop leest wat er binnenkomt en print dit
        public void ReaderThread()
        {
                while (true)
                {
                    string input = Read.ReadLine();
                    string type = input.Split(' ')[0]; //eerste woord verteld wat we moeten doen

                    if (type == "Print") //print een bericht
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 2);
                        Console.WriteLine(delen[1]);

                    }
                    if (type == "Buur") //nieuwe buur
                    {
                        int poort = int.Parse(input.Split()[1]);

                        Program.EditPath(poort, poort.ToString(), 1); //pad naar nieuwe buur is 1
                        Program.StuurBuren(Program.Buren[poort]); //stuur hem mijn buren

                    }
                    if (type == "Delete") //delete buur
                    {
                        int poort = int.Parse(input.Split()[1]);
                        Program.Delete(poort);
                    }
                    if (type == "Forward") //ontvang een pad van een buur
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 5);
                        int dest = int.Parse(delen[1]);
                        int _length = int.Parse(delen[2]);
                        string _closest = delen[3];
                        int poort = int.Parse(delen[4]); 
                    
                    if (_closest == "null")
                        { _closest = null; }

                        lock (Program.lockobj)  //zet pad in nested dictionary van alle paden van alle buren
                        {
                            if (!Program.RoutingTables.ContainsKey(dest) && _length <= 20) //als dit pad er nog niet in stond
                            {
                                Dictionary<int, Path> table = new Dictionary<int, Path>();
                                Program.RoutingTables.Add(dest, table);
                            }

                            Path pad = new Path() { closest = _closest, length = _length };
                            Program.RoutingTables[dest][poort] = pad;
                        
                        }

                        Program.EditPath(dest, poort.ToString(), (_length + 1)); //kijk of je eigen pad aangepast moet
                    }
                    if (type == "Doorsturen") //ontvang doorgestuurd bericht
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 4);
                        int poort = int.Parse(delen[1]);
                        string bericht = delen[3];
                        Program.Bericht(poort, bericht);
                    }
                    if (type == "GetPath") //geef je pad aan een buur
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
                    if (type == "ForwardDelete") //je buur heeft een verbinding verbroken dus je moet je eigen paden controleren
                    {
                        string[] delen = input.Split(new char[] { ' ' }, 3);
                        int dest = int.Parse(delen[1]);
                        string closest = delen[2].ToString();
                        Program.ForwardDelete(dest, closest);
                    }
                    if(type == "DeletePath")
                    {
                    int poort = int.Parse(input.Split(' ')[1]);
                    Program.Paden[poort].closest = null;
                    Program.Paden[poort].length = 25;
                    }
                }
            
            
        }
    }
}