using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NetwProg
{
    public class Program
    {
        static public int MijnPoort;

        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>(); //dictionary van buren
        static public Dictionary<int, Path> Paden = new Dictionary<int, Path>(); // en een van kortste paden naar anderen

        static Thread[] threads;
        static readonly object lockobj = new object(); //lock

        static void Main(string[] args)
        {
            MijnPoort = int.Parse(args[0]);
            Console.Title = "NetChange " + MijnPoort;
            new Server(MijnPoort);

            threads = new Thread[args.Length]; //threads maken voor elke verbinding

            Paden.Add(MijnPoort, new Path() { length = 0, closest = "local" });

            for(int i = 1; i < args.Length; i++)
            {
                int buur = int.Parse(args[0]);
                if (!Buren.ContainsKey(buur) && !(buur <= MijnPoort))
                {
                    Connect(buur);
                }
            }

            /*foreach (KeyValuePair<int, Connection> buur in Buren) //nu dat je verbindingen hebt met al je buren, schrijf het pad naar je buren op
            {
                int poort = buur.Key;
                EditPath(poort, poort.ToString(), 1);
            }*/


            while (true)
            {
                string input = Console.ReadLine();
                if(input.StartsWith("R"))
                {
                    //toon routing table


                    foreach(KeyValuePair<int, Path> pad in Paden) // print alle paden
                    {
                        Console.WriteLine(pad.Key + " " + pad.Value.length + " " + pad.Value.closest);
                    }

                    Console.WriteLine("Buren:");
                    foreach(KeyValuePair<int, Connection> buur in Buren)
                    {
                        Console.WriteLine(buur.Key);
                    }
                }
                if(input.StartsWith("C"))
                {
                    //Connect

                    int poort = int.Parse(input.Split()[1]);
                    Connect(poort);

                    
                }
                if(input.StartsWith("B"))
                {
                    //stuur bericht

                    string[] delen = input.Split(new char[] { ' ' }, 3);
                    int poort = int.Parse(delen[1]);
                    if (!Buren.ContainsKey(poort))
                        Console.WriteLine("Poort " + poort + " is niet bekend");
                    else
                        Console.WriteLine("bericht verstuurd naar " + poort);
                        Buren[poort].Write.WriteLine("P " + MijnPoort + ": " + delen[2]);
                }
                if(input.StartsWith("D"))
                {
                    //Delete buur
                }
            }

            
        }

        public static void Connect(int poort)
        {
            // Leg verbinding aan (als client)
            if (Buren.ContainsKey(poort))
            { } //Console.WriteLine("Hier is al verbinding naar!");
            else
            {
                AddBuur(poort);
                EditPath(poort, poort.ToString(), 1);
            }

            Buren[poort].Write.WriteLine("B " + MijnPoort);
        }

        public static void AddBuur(int buur)
        {
            while (!Buren.ContainsKey(buur))
            {
                try
                {

                    Buren.Add(buur, new Connection(buur)); //voeg buur toe aan burenlijst
                    Console.WriteLine("Verbonden: " + (buur)); //zeg dat je verbonden bent
                    //EditPath(buur, buur.ToString(), 1);
             
                }
                catch { }
            }
        }

        public static void EditPath(int dest, string _closest, int _length)
        {
           
                if (!Paden.ContainsKey((dest)))
                {
                    Path pad = new Path() { length = _length, closest = _closest };
                    Paden.Add(dest, pad);
                }
                else
                {
                    if (_length < Paden[dest].length)
                    {
                        Paden[dest].length = _length;
                        Paden[dest].closest = _closest;
                    }

                }
                //Console.WriteLine("Verbonden: " + dest);
            
        }
    }
}
