using System;
using System.Linq;

using Algorithms.LA;

namespace LevelAncestorProblem.CLI;

public class Program
{
    public static int Main(string[] _)
    {
        // Manually test with: -1 0 0 1 1 2
        Console.WriteLine("Enter parents array with spaces:");
        var parents = Console.ReadLine().Split(' ').Select(int.Parse).ToArray();

        var la = new LevelAncestorOptimal(parents);

        while (true)
        {
            // Manually test with:
            // 5 0
            // 5 1
            // 5 2
            // 4 1
            // 4 3
            Console.WriteLine("\nEnter test:");
            var currentTestStrings = Console.ReadLine().Split(' ');
            if (currentTestStrings.Length != 2)
            {
                break;
            }

            var currentTest = currentTestStrings.Select(int.Parse).ToArray();

            var ancestorIndex = la.Query(currentTest[0], currentTest[1]);

            Console.WriteLine($"AncestorIndex: {ancestorIndex}");
        }

        return 0;
    }
}
