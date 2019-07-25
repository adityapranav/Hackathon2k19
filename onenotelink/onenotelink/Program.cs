/*
	Copyright (c) 2019 Microsoft Corporation. All rights reserved. Licensed under the MIT license.
	See LICENSE in the project root for license information.
*/

using Microsoft.Graph;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace onenotelink
{
    class Program
    {
        static Logger logger = LogProvider.getLogInstance();

        static async Task Main(string[] args)
        {
            //validate(args);
            Console.WriteLine("Welcome to the OneNoteLink tool...!\n");

            try
            {
                //*********************************************************************
                // setup Microsoft Graph Client for user.
                //*********************************************************************
                AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

                // Check whether config. parameters have values
                config.CheckParameters();

                GraphApiProvider provider = new GraphApiProvider(logger);

                provider.CreateGraphSession(config);

                User u = await provider.GetCurrentUser();

                Console.WriteLine("Getting the Notebooks of "+ u.DisplayName);

                logger.Debug("Getting the Notebooks of User "+u.DisplayName);
                List<String> myNotebooks = await provider.GetNoteBooksForCurrentUser();
                logger.Debug("Successfully Retrieved the Notebooks of User " + u.DisplayName);

                int noteNumber = AskUserChoice(myNotebooks);

                await provider.FixBrokenLinks(noteNumber);
                Console.WriteLine("Done fixing up the entire notebook. Press Any key to exit!");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                // write log here 
                logger.Error("Exception Occurred in Program.Main! Message: "+ex.Message);
            }
        }

        private static int AskUserChoice(List<string> myNotebooks)
        {
            int i = 0;
            var map = new Dictionary<int, string>();
            foreach (string notebook in myNotebooks)
            {
                map.Add(i, notebook); 
                Console.WriteLine("[" + i++ + "]" + " " + notebook);
            }

            Console.WriteLine("Enter the Notebook to fix the broken Links!");
            int noteId = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine($"User Selected {map[noteId]}");
            logger.Info("Fixing the broken links of the Notebook "+map[noteId]);
            return noteId;
        }
    }
}
