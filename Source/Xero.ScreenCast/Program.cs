using System;

namespace XeroScreencast
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Console.WriteLine("Do you want to run as a public or private application?");
            Console.WriteLine(" Press 1 for a public application");
            Console.WriteLine(" Press 2 for a private application");
            Console.WriteLine(" Press 3 for a partner application");
            Console.WriteLine(" Press 4 for a load testing");

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            Console.WriteLine();

            if (keyInfo.KeyChar == '1')
            {
                Console.WriteLine("Running as a public application...");
                PublicApps.Run();
            }
            if (keyInfo.KeyChar == '2')
            {
                Console.WriteLine("Running as a private application...");
                PrivateApps.Run();
            } 
            if (keyInfo.KeyChar == '3')
            {
                Console.WriteLine("Running as a partner application...");
                PartnerApps.Run();
            }
            if (keyInfo.KeyChar == '4')
            {
                Console.WriteLine("Running load tests...");
                LoadTesting.Run();
            }

            Console.WriteLine("");
            Console.WriteLine(" Press Enter to Exit");
            Console.ReadLine();
        }

    }
}
