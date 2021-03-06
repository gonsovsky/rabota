using System;
using System.Configuration;

namespace CsudConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //CSUD_Mongo
            //CSUD_Postgre
            var csud = new Csud.Csud(ConfigurationManager.ConnectionStrings["CSUD_Postgre"].ConnectionString);

            //Правила доступа
            foreach (var p in csud.Summary.Overview())
            {
                Console.WriteLine(p);
                Console.WriteLine();
            }

            Console.ReadKey();

            //Персоны
            foreach (var p in csud.Persons.GetList())
            {
                Console.WriteLine(p.FirstName);
            }

            Console.ReadKey();
        }
    }
}
