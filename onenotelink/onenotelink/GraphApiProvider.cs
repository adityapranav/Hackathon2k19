using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;
using NLog;

namespace onenotelink
{
    class GraphApiProvider
    {
        private GraphServiceClient client = null;
        private User currentUser = null;
        private Dictionary<int, Notebook> notebookMap = null;
        private PageContentCrawler pageCrawler = null;
        private Logger logger = null;

        public GraphApiProvider(Logger logger)
        {
            this.logger = logger;
        }

        public void CreateGraphSession(AuthenticationConfig config)
        {
            try
            {
                logger.Debug("Before creating thh Graph client session. clientId :{0}", config.ClientId);
                this.client =
                    GraphClientFactory.GetGraphServiceClient(config.ClientId, config.Authority, config.Scopes);
            }
            catch (Exception e)
            {
                logger.Error("Exception occurred while creating graph client session!");
            }
        }

        public async Task<List<string>> GetNoteBooksForCurrentUser()
        {
            if (client == null)
            {
                logger.Error("Cannot Proceed with getting Notebooks without creating a session. Please use CreateGraphSession");
                throw new Exception("Please create a GraphClient session first");
            }

            var myNotebooks = await client.Me.Onenote.Notebooks.Request().GetAsync();

            List<string> resultNotebooks = new List<string>();
            notebookMap = new Dictionary<int, Notebook>();
            int i = 0;
            foreach (var notebook in myNotebooks)
            {
                notebookMap.Add(i, notebook);
                resultNotebooks.Add(notebook.DisplayName);
                i++;
            }
            return resultNotebooks;
        }

        public async Task<User> GetCurrentUser()
        {
            currentUser = await client.Me.Request().GetAsync();
            return currentUser;
        }

        public async Task FixBrokenLinks(int selectedNotebookNumber)
        {
            logger.Info("Processing the Notebook " + notebookMap[selectedNotebookNumber].DisplayName);
            INotebookSectionsCollectionPage mySections = await GetSections(notebookMap[selectedNotebookNumber]);

            foreach (OnenoteSection section in mySections)
            {
              logger.Info("Processing Section " + section.DisplayName);
              IOnenoteSectionPagesCollectionPage pageList = await GetPagesInSection(section);
              foreach (OnenotePage page in pageList)
              {
                  logger.Info("Processing Page "+page.Title+" in the Section " +section.DisplayName);
                  await FixBrokenLinksInPage(page);
              }
            }
        }

        private async Task<INotebookSectionsCollectionPage> GetSections(Notebook selectedNotebook)
        {
            try
            {
                var sections = await client.Me.Onenote.Notebooks[selectedNotebook.Id].Sections.Request().GetAsync();
                return sections;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private async Task<IOnenoteSectionPagesCollectionPage> GetPagesInSection(OnenoteSection section)
        {
            var pages = await client.Me.Onenote.Sections[section.Id].Pages.Request().GetAsync();
            return pages;
        }

        private async Task FixBrokenLinksInPage(OnenotePage page)
        {
            List<QueryOption> options = new List<QueryOption>
            {
                new QueryOption("includeIDs", "true")
            };

            Stream content = await client.Me.Onenote.Pages[page.Id].Content.Request(options).GetAsync();
            StreamReader sr = new StreamReader(content);
            pageCrawler = new PageContentCrawler();

            List<ReplacementData> updateRequestData = pageCrawler.GetBrokenLinkIds(sr.ReadToEnd(), "msft.spoppe.com", "microsoft.sharepoint-df.com");
            try
            {
                if (updateRequestData.Count > 0)
                {
                    Console.WriteLine("Broken Link found in page {0}. Number of Broken Links {1}", page.Title,
                        updateRequestData.Count);

                    logger.Info("Broken Link found in page {0}. Number of Broken Links {1}", page.Title,
                        updateRequestData.Count);
                    //UpdatePageUsingGraphClientApi(page, updateRequestData)
                    UpdatePageUsingPlainHttp(page, updateRequestData);
                }
                else
                {
                    logger.Info("page {0} has no Broken Links! Proceeding to the Next Page", page.Title);
                }
            }
            catch (Exception e)
            {
                logger.Error("Exception Occured while sending explicit HttpRequest to update the page. Exception: " +
                             e.Message);
            }
        }

        private async void UpdatePageUsingPlainHttp(OnenotePage page, List<ReplacementData> updateRequestData)
        {
            string graphURL = "https://graph.microsoft.com/v1.0/users/" + currentUser.Id + "/onenote/pages/" + page.Id + "/content";
            string patchBody = preparePatchStringRequestBody(updateRequestData);
            sendHttpRequest(graphURL, patchBody);
            Console.WriteLine("page {0} is fixed up!", page.Title);
            logger.Info("page {0} is fixed up!", page.Title);
        }

        private void sendHttpRequest(string url, string jsonBody)
        {
            Uri endpointUri = new Uri(url);
            HttpClient httpClient = new System.Net.Http.HttpClient();
            HttpRequestMessage request = new HttpRequestMessage { RequestUri = endpointUri };
            request.Method = new HttpMethod("PATCH");
            client.AuthenticationProvider.AuthenticateRequestAsync(request);
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
            var response = httpClient.SendAsync(request).Result;
        }


        private async void UpdatePageUsingGraphClientApi(OnenotePage page, List<ReplacementData> updateRequestData)
        {
            try
            {
                using (var stream = new OneNoteMemoryStream(preparePatchStringRequestBody(updateRequestData)
                    .Select(c => (byte)c).ToArray()))
                {
                    var pages = new OnenotePage()
                    {
                        Content = stream
                    };
                    await client.Me.Onenote.Pages[page.Id].Request().UpdateAsync(pages);
                }
            }
            catch (Exception e)
            {
                logger.Error("Unable to fix the broken link in the page {0}. Exception: {1}", page.Title, e.Message);
            }
            logger.Info("page {0} is fixed up!", page.Title);
        }

        private string preparePatchStringRequestBody(List<ReplacementData> replacementData)
        {
            StringBuilder sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartArray();
                {
                    foreach (ReplacementData data in replacementData)
                    {
                        writer.WriteStartObject();
                        {
                            writer.WritePropertyName("Target");
                            writer.WriteValue(data.Target);
                            writer.WritePropertyName("Action");
                            writer.WriteValue("insert");
                            writer.WritePropertyName("Position");
                            writer.WriteValue("after");
                            writer.WritePropertyName("Content");
                            writer.WriteValue(data.Content);
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
            }
            //Console.WriteLine(sb.ToString());
            return sb.ToString();
        }
    }
}