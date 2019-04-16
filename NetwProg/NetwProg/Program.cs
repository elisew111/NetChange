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
        
        static readonly object lockobj = new object(); //lock

        static void Main(string[] args)
        {
            MijnPoort = int.Parse(args[0]);
            Console.Title = "NetChange " + MijnPoort;
            new Server(MijnPoort);
            

            Paden.Add(MijnPoort, new Path() { length = 0, closest = "local" });

            for(int i = 1; i < args.Length; i++)
            {
                int buur = int.Parse(args[i]);
                if (!Buren.ContainsKey(buur) && !(buur <= MijnPoort))
                {
                    Connect(buur);
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
                if (input.StartsWith("C"))
                {
                    //Connect
                    lock (lockobj)
                    {
                        int poort = int.Parse(input.Split()[1]);
                        Connect(poort); 
                    }


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
                    Buren[poort].Write.WriteLine("Delete " + MijnPoort);
                    Buren.Remove(poort);
                    DeletePaths(poort.ToString());
                    Console.WriteLine("Verbroken: " + poort);

                    //TODO: vind nieuwe pad
                }
            }
        }

        public static void DeletePaths(string poort)
        {
            foreach (KeyValuePair<int, Path> pad in Paden)
            {
                if (pad.Value.closest == poort);
                {
                    int key = pad.Key;
                    Paden[key].length = 25; //TODO: Deze echt verwijderen
                    foreach (KeyValuePair<int, Connection> buur in Buren)
                    {
                        buur.Value.Write.WriteLine("GetPath " + MijnPoort + " " + poort);
                    }
                }
            }
        }
        

        public static void Bericht(int poort, string bericht)
        {
            if (!Buren.ContainsKey(poort))
            {
                if (!Paden.ContainsKey(poort) || Paden[poort].length > 20)
                {
                    Console.WriteLine("Poort " + poort + " is niet bekend");
                    return;
                }
                else
                {
                    Buren[int.Parse(Paden[poort].closest)].Write.WriteLine("Doorsturen " + poort + " " + MijnPoort + ": " + bericht);
                    
                }
            }
            else
            {
                //Console.WriteLine("bericht verstuurd naar " + poort);
                Buren[poort].Write.WriteLine("Print " + bericht);
            }
            Console.WriteLine("Bericht voor " + poort + " doorgestuurd naar " + Paden[poort].closest);
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

            Buren[poort].Write.WriteLine("Buur " + MijnPoort);
        }

        public static void AddBuur(int buur)
        {

            while (!Buren.ContainsKey(buur))
            {
                try
                {
                    Connection nieuweBuur = new Connection(buur);
                    Buren.Add(buur, nieuweBuur); //voeg buur toe aan burenlijst
                    Console.WriteLine("Verbonden: " + (buur)); //zeg dat je verbonden bent
                    StuurBuren(nieuweBuur);
             
                }
                catch { }
            }
        }
        

        public static void StuurBuren(Connection buur) //stuur alle paden naar 1 buur
        {
            foreach(KeyValuePair<int, Path> pad in Paden)
            {
                StuurPad(buur, pad);
            }
        }

        public static void StuurPad(Connection buur, KeyValuePair<int, Path> pad) //stuur 1 pad naar 1 buur
        {
            /*string _closest;
            if (pad.Value.closest == "local")
            { _closest = MijnPoort.ToString(); }
            else
            { _closest = pad.Value.closest; }*/

            buur.Write.WriteLine("Forward " + pad.Key + " " + pad.Value.length + " " + MijnPoort.ToString());
        }

        public static void EditPath(int dest, string _closest, int _length)
        {
            if (Paden.ContainsKey(dest) && _length > Paden[dest].length)
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
                Console.WriteLine("Afstand naar " + dest + " is nu " + _length + " via " + _closest);
            }


            if (_length < 20)
            {
                foreach (KeyValuePair<int, Connection> buur in Buren)
                {
                    buur.Value.Write.WriteLine("Forward " + dest + " " + _length + " " + MijnPoort);
                }
                //Console.WriteLine("Verbonden: " + dest);
            }
            
        }
    }
}
