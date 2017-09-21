using System;

namespace Ranger
{
    class Program
    {
        private const string originName = "Istanbul, Turkey";
        private const int rangeMins = 180;
        private const int gridSize = 35;
        private const int smoothPct = 60;

        static void Main(string[] args)
        {
            var range = new RangeGrid(originName, rangeMins, gridSize);

            range.Init();

            range.Process();

            range.CreateBorder();

            range.CreateDynamicMap(smoothPct);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
