using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;

namespace onenotelink
{
    internal class PageContentCrawler
    {
        public List<ReplacementData> GetBrokenLinkIds(string pageContent, string brokenSite, string originalSite)
        {
            List<ReplacementData> ids = new List<ReplacementData>();

            string pattern = "<p(.*)</p>";

            MatchCollection matches = Regex.Matches(pageContent, pattern);

            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    string text = m.Groups[1].Value;
                    if (text.IndexOf("href") != -1)
                    {
                        string pattern1 = "id=\"(.*)\"";
                        MatchCollection matches1 = Regex.Matches(text, pattern1);

                        if (matches1.Count > 0)
                        {
                            foreach (Match m1 in matches1)
                            {
                                string subText = m1.Groups[1].Value;
                                if (isBrokenLink(text, brokenSite))
                                {
                                    string paraID   = ExtractPara(subText);
                                    string cleanURL = ExtractSaneURL(text.Replace(brokenSite, originalSite)); 
                                    ReplacementData d = new ReplacementData(paraID, cleanURL);
                                    ids.Add(d);
                                }
                            }
                        }
                    }
                }
            }

            return ids;
        }

        private bool isBrokenLink(string link, string brokenSite)
        {
            return link.Contains(brokenSite);
        }

        private string ExtractPara(string text)
        {
            int startIndex = text.IndexOf("p:{");
            int midIndex = text.IndexOf("}");

            int i = midIndex+1;
            while(i < text.Length)
            {
                if (text[i] == '}')
                {
                    break;
                }
                i++;
            }

            return text.Substring(startIndex, i + 1);
        }

        private string ExtractSaneURL(string fixedURL)
        {
            int startIndex = fixedURL.IndexOf("<a");
            return fixedURL.Substring(startIndex);
        }
    }
}