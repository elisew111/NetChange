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

        static public Dictionary<int, Dictionary<int, Path>> RoutingTables = new Dictionary<int, Dictionary<int, Path>>(); //nested dictionary met voor elke bestemming een dictionary met voor elke buur hun pad naar de bestemming
        
        static public readonly object lockobj = new object(); //lock
        
        static void Main(string[] args)
        {
            MijnPoort = int.Parse(args[0]);
            Console.Title = "NetChange " + MijnPoort;
            new Server(MijnPoort);
            
            Paden.Add(MijnPoort, new Path() { length = 0, closest = "local" }); //pad naar mezelf

            for (int i = 1; i < args.Length; i++)
            {
                int buur = int.Parse(args[i]);
                lock (lockobj)
                {
                    if (buur > MijnPoort)
                    {
                        Connect(buur);  //verbind met alle opgegeven buren
                    }
                }
            }
            
            new Thread(() => ReadLoop()).Start();   //thread voor readloop



        }

        private static void ReadLoop()  //lees en verwerk console input
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (input.StartsWith("R"))
                {
                    //toon routing table

                    lock (lockobj)
                    {
                        foreach (KeyValuePair<int, Path> pad in Paden) 
                        {
                            if (pad.Value.length <= 20) //print alle paden die niet 'oneindig' lang zijn
                            { Console.WriteLine(pad.Key + " " + pad.Value.length + " " + pad.Value.closest); }
                        }

                        Console.WriteLine("//Buren:"); //voor debug doeleinden: laat zien met welke buren je verbonden bent

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
                        Buren[poort].Write.WriteLine("Delete " + MijnPoort); //laat eerst je buur jouw deleten, want als je hem eerst delete kun je hem geen bericht meer sturen
                        Delete(poort);
                    }


                }
            }
        }

        public static void Delete(int poort)    //verbreek de verbinding met een buur
        {
            lock (lockobj)
            {
                if (Buren.ContainsKey(poort))   //verwijder de buur uit Buren
                {
                    Buren.Remove(poort);
                }

                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Value.closest == poort.ToString())  //voor alle paden die je hebt waarvoor je door je verwijderde buur ging:
                    {
                        Paden[pad.Key].length = 25;             // maak de lengte hoog en closest null, paden met deze waarden worden genegeerd
                        Paden[pad.Key].closest = null;

                        foreach (KeyValuePair<int, Connection> buur in Buren)
                        {
                            //buur.Value.Write.Write("Forward " + pad.Key + " " + 25 + " " + null + " " + MijnPoort);       // Stuur je buur dit nieuwe pad zodat hij dit in zijn nested dictionary kan zetten. Dit veroorzaakt helaas een crash.
                            buur.Value.Write.WriteLine("ForwardDelete " + pad.Key + " " + MijnPoort); //Laat je buren ook paden via jouw langs je verbroken verbinding verwijderen
                        }

                    }
                }

                VindNieuwePaden(); //vervang 25 en null met betere waarden als er een ander pad is

                foreach (KeyValuePair<int, Connection> buur in Buren)
                {
                    StuurBuren(buur.Value);     //stuur je nieuwe routingtable naar al je buren
                }


            }
            Console.WriteLine("Verbroken: " + poort);

        }

        public static void VindNieuwePaden()
        {
            foreach (KeyValuePair<int, Path> pad in Paden)
            {
                if (pad.Value.closest == null)  // voor elk pad dat 'verwijderd' is
                {
                    int _length = 25;
                    string _closest = null;
                    foreach (KeyValuePair<int, Path> table in RoutingTables[pad.Key]) //kijk of er een korter pad te vinden is
                    {
                        if (Buren.ContainsKey(table.Key))
                        {
                            if (table.Value.length < _length)   //zoja vervang de waarden met de waarden van dat pad
                            {
                                _length = table.Value.length + 1;
                                _closest = table.Key.ToString();
                            }
                        }
                    }

                    EditPath(pad.Key, _closest, _length); //verander het pad en stuur door

                    if (_closest == null)   //als je geen beter pad hebt gevonden
                    {
                        foreach(KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.WriteLine("DeletePath " + pad.Key);    //zeg tegen al je buren dat er geen pad meer bestaat en er dus een netwerkpartitie is
                        }
                    }
                }
            }
            
        }


        public static void ForwardDelete(int dest, string _closest) //Verwijder recursief alle paden die langs een verbroken verbinding gaan
        {
            lock (lockobj)
            {
                foreach (KeyValuePair<int, Path> pad in Paden)
                {
                    if (pad.Key == dest && pad.Value.closest == _closest)   //als je via de vorige persoon naar de bestemming moet volgens je pad, dan kan dat niet meer door de verbroken verbinding tussen de vorige en de bestemming.
                    {
                        Paden[dest].closest = null; //verwijder dus ook jouw pad
                        Paden[dest].length = 25;

                        foreach (KeyValuePair<int, Connection> buur in Buren)
                        {
                            buur.Value.Write.WriteLine("ForwardDelete " + dest + " " + MijnPoort.ToString()); //en laat jouw buren dit ook weer doen
                        }
                    }
                }
            }
        }

        public static void Bericht(int poort, string bericht)   //stuur een bericht naar bestemming
        {
            lock (lockobj)
            {
                if (!Buren.ContainsKey(poort))  //als bestemming niet je directe buur is
                {
                    if (!Paden.ContainsKey(poort) || Paden[poort].closest == null) //als er geen pad naar is
                    {
                        Console.WriteLine("Poort " + poort + " is niet bekend");
                        return;
                    }
                    else //stuur de dichtstbijzijnde buur dat ie het bericht moet doorsturen naar de bestemming
                    {
                        Buren[int.Parse(Paden[poort].closest)].Write.WriteLine("Doorsturen " + poort + " " + MijnPoort + ": " + bericht);
                        Console.WriteLine("Bericht voor " + poort + " doorgestuurd naar " + Paden[poort].closest);
                    }
                }

                else //als het je buur is: schrijf meteen naar je buur
                {
                    Buren[poort].Write.WriteLine("Print " + bericht);
                    Console.WriteLine("Bericht voor " + poort + " doorgestuurd naar " + Paden[poort].closest);
                }


            }

        }

        public static void Connect(int poort) //verbind met een nieuwe buur
        {
            if (!Buren.ContainsKey(poort))
            {
                AddBuur(poort); // voeg de buur toe
                EditPath(poort, poort.ToString(), 1);   //pad naar hem is nu 1
                Buren[poort].Write.WriteLine("Buur " + MijnPoort);
            }
            else
            {
                StuurBuren(Buren[poort]);   //als het al je buur was stuur dan nog eens je paden door
            }
        }

        public static void AddBuur(int buur)    //voef buur toe en stuur je paden door
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


        public static void EditPath(int dest, string _closest, int _length) //vergelijk pad met huidige pad en pas zo nodig aan
        {
            lock (lockobj)
            {

                if ((Paden.ContainsKey(dest) && _length > Paden[dest].length)) //pas niet aan als je eigen pad korter is
                { return; }
                else
                {
                    if (!Paden.ContainsKey((dest))) //maak een nieuw pad als je hier nog geen pad naar had
                    {
                        Path pad = new Path() { length = _length, closest = _closest };
                        Paden.Add(dest, pad);

                        
                    }
                    else        //pas anders alleen de waarden aan
                    {
                        Paden[dest].length = _length;
                        Paden[dest].closest = _closest;

                        
                    }
                    //Console.WriteLine("Afstand naar " + dest + " is nu " + _length + " via " + _closest);         <- dit moet voor de opdracht maar maakt de output erg groot
                }

                {
                    foreach (KeyValuePair<int, Connection> buur in Buren)
                    {
                        buur.Value.Write.WriteLine("Forward " + dest + " " + _length + " " + _closest + " " + MijnPoort);   //stuur je nieuwe pad door naar al je buren
                    }
                }

                if (_length > 20 && Paden.ContainsKey(dest))    //als je pad te lang wordt gaan we er vanuit dat er geen pad is
                {
                    Paden[dest].length = 25;
                    Paden[dest].closest = null;
                    //VindNieuwePaden();        <- Zoek betere paden,  veroorzaakt stackoverflow exception
                }

            }

        }
    }
}
