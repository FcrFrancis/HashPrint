using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace Service.Rfid
{
    public class Response
    {
        /// <summary>
        /// 业务状态码，0:成功，其他:失败
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("msg")]
        public string Message { get; set; }
    }

    public class Response<T> : Response
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
