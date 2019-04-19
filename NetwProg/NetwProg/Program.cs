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

        static public Dictionary<int, Dictionary<int, Path>> RoutingTables = new Dictionary<int, Dictionary<int, Path>>();

        //TODO: onthouden welke paden je als laatst gehoord hebt van elke buur

        static public readonly object lockobj = new object(); //lock

        public static int size;

        static void Main(string[] args)
        {
            MijnPoort = int.Parse(args[0]);
            Console.Title = "NetChange " + MijnPoort;
            new Server(MijnPoort);

            size = args.Length - 1;

        Paden.Add(MijnPoort, new Path() { length = 0, closest = "local" });

            for (int i = 1; i < args.Length; i++)
            {
                int buur = int.Parse(args[i]);
                lock (lockobj)
                {
                    if (buur > MijnPoort)
                    {
                        Connect(buur);
                    }
                }
            }

            new Thread(() => ReadLoop()).Start();



        }

        private static void ReadLoop()
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (input.StartsWith("R"))
                {
                    //toon routing table

                    lock (lockobj)
                    {
                        foreach (KeyValuePair<int, Path> pad in Paden) // print alle paden
                        {
                            if (pad.Value.closest != null)
                            { Console.WriteLine(pad.Key + " " + pad.Value.length + " " + pad.Value.closest); }
                        }

                        Console.WriteLine("//Buren:");

                        foreach (KeyValuePair<int, Connection> buur in Buren)
                        {
                            Console.WriteLine("//" + buur.Key);
                        }
                    }
                }
                if (input.StartsWith("C"))
                {
                    //Connect
                    int poort = int.Parse(input.Split()[1]);
                    Connect(poort);


                }
                if (input.StartsWith("B"))
                {
                    //stuur bericht

                    string[] delen = input.Split(new char[] { ' ' }, 3);
                    int poort = int.Parse(delen[1]);
                    Bericht(poort, delen[2]);
                }
                if (input.StartsWith("D"))
                {
                    //Delete buur
                    int poort = int.Parse(input.Split()[1]);
                    if (Buren.ContainsKey(poort))
                    {
                        Buren[poort].Write.WriteLine("Delete " + MijnPoort);
                        Delete(poort);
                    }


                }
            }
        }

        public static void Delete(int poort)
        {
            lock (lockobj)
            {
                if (Buren.ContainsKey(poort))
                {
                    Buren.Remove(poort);
                }

                //DeletePaths(poort.ToString());

                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Value.closest == poort.ToString())
                    {
                        Paden[pad.Key].length = size;
                        Paden[pad.Key].closest = null;

                        foreach (KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.WriteLine("ForwardDelete " + pad.Key + " " + MijnPoort);
                        }

                    }
                }

                VindNieuwePaden();

                foreach (KeyValuePair<int, Connection> buur in Buren)
                {
                    StuurBuren(buur.Value);
                }


            }
            Console.WriteLine("Verbroken: " + poort);

        }

        public static void VindNieuwePaden()
        {

            foreach (KeyValuePair<int, Path> pad in Paden)
            {
                if (pad.Value.closest ==)
                {
                    int _length = size;
                    string _closest = null;
                    foreach (KeyValuePair<int, Path> table in RoutingTables[pad.Key])
                    {
                        if (Buren.ContainsKey(table.Key))
                        {
                            if (table.Value.length < _length)
                            {
                                _length = table.Value.length + 1;
                                _closest = table.Key.ToString();
                            }
                        }
                    }
                    if (_length > size)
                    {
                        //toDelete.Add(pad.Key);
                        Paden[pad.Key].length = size;
                        Paden[pad.Key].closest = null;
                        foreach(KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.WriteLine("DeletePath " + pad.Key);
                        }
                    }
                    else
                    {
                        Paden[pad.Key].length = _length;
                        Paden[pad.Key].closest = _closest;
                    }
                }
            }
            
        }


        public static void ForwardDelete(int dest, string _closest)
        {
            lock (lockobj)
            {
                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Key == dest && pad.Value.closest == _closest)
                    {
                        Paden[pad.Key].length = size;
                        Paden[pad.Key].closest = null;

                        foreach (KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.WriteLine("ForwardDelete " + dest + " " + MijnPoort.ToString());
                        }
                    }
                }
            }
        }

        public static void Bericht(int poort, string bericht)
        {
            lock (lockobj)
            {
                if (!Buren.ContainsKey(poort))
                {
                    if (!Paden.ContainsKey(poort) || Paden[poort].closest == null)
                    {
                        Console.WriteLine("Poort " + poort + " is niet bekend");
                        return;
                    }
                    else
                    {
                        Buren[int.Parse(Paden[poort].closest)].Write.WriteLine("Doorsturen " + poort + " " + MijnPoort + ": " + bericht);
                        Console.WriteLine("Bericht voor " + poort + " doorgestuurd naar " + Paden[poort].closest);
                    }
                }

                else
                {
                    //Console.WriteLine("bericht verstuurd naar " + poort);
                    Buren[poort].Write.WriteLine("Print " + bericht);
                    Console.WriteLine("Bericht voor " + poort + " doorgestuurd naar " + Paden[poort].closest);
                }


            }

        }

        public static void Connect(int poort)
        {
            if (!Buren.ContainsKey(poort))
            {
                AddBuur(poort);
                EditPath(poort, poort.ToString(), 1);
                Buren[poort].Write.WriteLine("Buur " + MijnPoort);
            }
            else
            {
                StuurBuren(Buren[poort]);
            }
        }

        public static void AddBuur(int buur)
        {
            lock (lockobj)
            {
                while (!Buren.ContainsKey(buur))
                {
                    try
                    {
                        Connection nieuweBuur = new Connection(buur);
                        Buren.Add(buur, nieuweBuur); //voeg buur toe aan burenlijst
                        Console.WriteLine("Verbonden: " + (buur)); //zeg dat je verbonden bent

                    }
                    catch { }
                }

                StuurBuren(Buren[buur]);
            }
        }


        public static void StuurBuren(Connection buur) //stuur alle paden naar 1 buur
        {
            lock (lockobj)
            {
                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Value.closest != null)
                    {
                        buur.Write.WriteLine("Forward " + pad.Key + " " + pad.Value.length + " " + pad.Value.closest + " " + MijnPoort.ToString());
                    }
                }
            }
        }


        public static void EditPath(int dest, string _closest, int _length)
        {
            lock (lockobj)
            {

                if ((Paden.ContainsKey(dest) && _length > Paden[dest].length) || _closest == null)
                { return; }
                else
                {
                    if (!Paden.ContainsKey((dest)) || Paden[dest].closest == null)
                    {
                        Path pad = new Path() { length = _length, closest = _closest };
                        Paden[dest] = pad;

                        //Console.WriteLine("Afstand naar " + dest + " is nu " + _length + " via " + _closest);
                    }
                    else
                    {
                        Paden[dest].length = _length;
                        Paden[dest].closest = _closest;

                        //Console.WriteLine("Afstand naar " + dest + " is nu " + _length + " via " + _closest);
                    }
                }

                {
                    foreach (KeyValuePair<int, Connection> buur in Buren)
                    {
                        buur.Value.Write.WriteLine("Forward " + dest + " " + _length + " " + _closest + " " + MijnPoort);
                    }
                }
                //Console.WriteLine("Verbonden: " + dest);

                if (_length > size && Paden.ContainsKey(dest))
                {
                    Paden[dest].closest = null;
                    Paden[dest].length = size;
                    VindNieuwePaden();
                }

            }

        }
    }
}
