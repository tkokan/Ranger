using System;

namespace Ranger
{
    class Program
    {
        static void Main(string[] args)
        {
            var ranger = new Ranger();

            var mapAreaInputs = new MapAreaInputs()
            {
                OriginName = "Rome, Italy"
            };
            
            var mapInputs = new MapInputs();

            ranger.CreateDynamicMap(mapAreaInputs, mapInputs);

            var area = ranger.CalculateArea(mapAreaInputs);

            Console.WriteLine("Area = {0:0.00} sq km", area);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
