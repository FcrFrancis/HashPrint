using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UHFDemo.Model
{
    public class RequestModel
    {
   

        public string RFIDStr { get; set; }
        public List<RFIDModel> RFIDInfo { get; set; }
    }
}
