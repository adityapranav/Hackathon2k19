/*
	Copyright (c) 2019 Microsoft Corporation. All rights reserved. Licensed under the MIT license.
	See LICENSE in the project root for license information.
*/

using Microsoft.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace onenotelink
{
    class Programold
    {
        static async Task MainOld(string[] args)
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

                var graphServiceClient = GraphClientFactory.GetGraphServiceClient(config.ClientId, config.Authority, config.Scopes);

                if (graphServiceClient != null)
                {
                    var user = await graphServiceClient.Me.Request().GetAsync();
                    string userId = user.Id;
                    string mailAddress = user.UserPrincipalName;
                    string displayName = user.DisplayName;
                    Console.WriteLine("Display Name: " + displayName);

                    var mynotebooks = await graphServiceClient.Me.Onenote.Notebooks.Request().GetAsync();
                    Console.WriteLine("\nList of your Notebooks: \n");
                    int i = 0;
                    foreach (var notebook in mynotebooks)
                    {
                        Console.WriteLine(" [" + i + "] " + notebook.DisplayName);
                        i++;
                    }
                    Console.Write("\nSelect Notebook to Fix: ");
                    string val = Console.ReadLine();

                    int index = Convert.ToInt32(val);

                    i = 0;
                    Microsoft.Graph.Notebook selectedNotebook = null;
                    foreach (var notebook in mynotebooks)
                    {
                        selectedNotebook = notebook;
                        if (i == index) break;
                        i++;
                    }

                    List<QueryOption> options = new List<QueryOption>
                                {
                                     new QueryOption("includeIDs", "true")
                                };

                    var mysections = await graphServiceClient.Me.Onenote.Notebooks[selectedNotebook.Id].Sections.Request().GetAsync();
                    foreach (var section in mysections)
                    {
                        var mypages = await graphServiceClient.Me.Onenote.Sections[section.Id].Pages.Request().GetAsync();
                        foreach (var page in mypages)
                        {
                            Console.WriteLine("=====================Title: " + page.Title + "=====================");
                            Stream content = await graphServiceClient.Me.Onenote.Pages[page.Id].Content.Request(options).GetAsync();
                            StreamReader sr = new StreamReader(content);
                            Console.WriteLine("Content: " + sr.ReadToEnd());
                            Console.WriteLine("===============================================================");
                        }
                    }

                    /*var groups = await graphServiceClient.Me.MemberOf.Request().GetAsync();
                    foreach(var group in groups)
                    {
                        Console.WriteLine(group.Id);
                    }*/

                    Console.WriteLine("Completed...");
                    Console.ReadKey();
                }
                else
                {
                    throwError("We weren't able to create a GraphServiceClient for you. Please check the output for errors.");
                    return;
                }
            }
            catch (ArgumentNullException ex)
            {
                throwError(ex.Message + "\nPlease follow the Readme instructions for configuring this application.");
                return;
            }
            catch (FileNotFoundException)
            {

                throwError("The configuration file 'appsettings.json' was not found. " +
                                  "Rename the file 'appsettings.json.example' in the solutions folder to 'appsettings.json'." +
                                  "\nPlease follow the Readme instructions for configuring this application.");
                return;
            }
            catch (Exception ex)
            {
                string msg = "Connecting to graph failed with the following message: {0}" + ex.Message;
                if (ex.InnerException != null)
                {
                    msg = msg + "\n Error detail: {0}" + ex.InnerException.Message;
                }
                throwError(msg);
                return;
            }
        }

        private static void validate(string[] args)
        {
            if (args.Length < 1)
            {
                throwError("Please provide OneNote notebook link.");
                Environment.Exit(1);
            }
            else
            {
                if (!System.Uri.IsWellFormedUriString(args[0], UriKind.Absolute))
                {
                    throwError("Please provide a valid url.");
                    Environment.Exit(1);
                }
            }
        }

        private static void throwError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ResetColor();
            Console.ReadKey();
        }
    }
}