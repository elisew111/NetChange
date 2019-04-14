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

            Paden.Add(MijnPoort, new Path() { length = 0, closest = "local" });


            while(true)
            {
                string input = Console.ReadLine();
                if(input.StartsWith("R"))
                {
                    //toon routing table

                    foreach(KeyValuePair<int, Path> pad in Paden) // print alle paden
                    {
                        Console.WriteLine(pad.Key + " " + pad.Value.length + " " + pad.Value.closest);
                    }
                }
                if(input.StartsWith("C"))
                {
                    //Connect
                    Program program = new Program();

                    int poort = int.Parse(input.Split()[1]);
                    program.AddBuur(poort);
                    Buren[poort].Write.WriteLine("B " + MijnPoort);
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

        public void AddBuur(int poort)
        {
            // Leg verbinding aan (als client)
            if (Buren.ContainsKey(poort))
            { } //Console.WriteLine("Hier is al verbinding naar!");
            else
            {
                Buren.Add(poort, new Connection(poort));
                if (!Paden.ContainsKey((poort)))
                {
                    Path pad = new Path() { length = 1, closest = poort.ToString() };
                    Paden.Add(poort, pad);
                }
                else
                {
                    Paden[poort].length = 1;
                    Paden[poort].closest = poort.ToString();

                }
                Console.WriteLine("Verbonden: " + poort);
            }
        }
    }
}
