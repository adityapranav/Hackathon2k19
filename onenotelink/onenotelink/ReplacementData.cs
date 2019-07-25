using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace onenotelink
{
    class ReplacementData
    {
        public string Target;
        public string Action;
        public string Position;
        public string Content;
    

        public ReplacementData(string paraId, string fixedUrl)
        {
            this.Target = paraId;
            this.Content = fixedUrl;
            this.Action = "insert";
            this.Position = "before";
        }
    }
}
