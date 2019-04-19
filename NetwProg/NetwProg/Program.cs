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

        static public int size;

        static void Main(string[] args)
        {
            MijnPoort = int.Parse(args[0]);
            Console.Title = "NetChange " + MijnPoort;
            new Server(MijnPoort);

            

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
                            if (pad.Value.length <= 20)
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

                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Value.closest == poort.ToString())
                    {
                        //EditPath(pad.Key, null, 25);
                        Paden[pad.Key].length = 25;
                        Paden[pad.Key].closest = null;

                        foreach (KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.Write("Forward " + pad.Key + " " + 25 + " " + null + " " + MijnPoort);
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
            //var toDelete = new List<int>();

            foreach (KeyValuePair<int, Path> pad in Paden)
            {
                //if (pad.Value.length > 20)
                if (pad.Value.closest == null)
                {
                    int _length = 25;
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

                    EditPath(pad.Key, _closest, _length);

                    if (_closest == null)
                    {
                        foreach(KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.WriteLine("DeletePath " + pad.Key);
                        }
                    }
                    //else
                    //{
                        //Paden[pad.Key].length = _length;
                        //Paden[pad.Key].closest = _closest;
                    //}
                }
            }

            /*foreach (int pad in toDelete)
            {
                //Paden.Remove(pad);
                Paden[pad].length = 25;
                Paden[pad].closest = null;
            }*/
        }


        public static void ForwardDelete(int dest, string _closest)
        {
            lock (lockobj)
            {
                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Key == dest && pad.Value.closest == _closest)
                    {
                        //EditPath(dest, null, 25);
                        Paden[dest].closest = null;
                        Paden[dest].length = 25;

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
                    buur.Write.WriteLine("Forward " + pad.Key + " " + pad.Value.length + " " + pad.Value.closest + " " + MijnPoort.ToString());
                }
            }
        }


        public static void EditPath(int dest, string _closest, int _length)
        {
            lock (lockobj)
            {

                if ((Paden.ContainsKey(dest) && _length > Paden[dest].length))
                { return; }
                else
                {
                    if (!Paden.ContainsKey((dest)))
                    {
                        Path pad = new Path() { length = _length, closest = _closest };
                        Paden.Add(dest, pad);

                        
                    }
                    else
                    {
                        Paden[dest].length = _length;
                        Paden[dest].closest = _closest;

                        
                    }
                    //Console.WriteLine("Afstand naar " + dest + " is nu " + _length + " via " + _closest);         <- dit maakt de output erg groot
                }

                {
                    foreach (KeyValuePair<int, Connection> buur in Buren)
                    {
                        buur.Value.Write.WriteLine("Forward " + dest + " " + _length + " " + _closest + " " + MijnPoort);
                    }
                }
                //Console.WriteLine("Verbonden: " + dest);

                if (_length > 20 && Paden.ContainsKey(dest))
                {
                    Paden[dest].length = 25;
                    Paden[dest].closest = null;
                    VindNieuwePaden();
                }

            }

        }
    }
}
