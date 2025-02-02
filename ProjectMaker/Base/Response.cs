using System.Net;

namespace ProjectMaker.Base
{
    public class Response<T>
    {
        public Response()
        {

        }
        public Response(T data, string message)
        {

        }
        public Response(string message)
        {

        }
        public Response(string message, bool succeeded)
        {

        }
        public HttpStatusCode StatusCode { get; set; }
        public object Meta { get; set; } = new object();
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public T? Data { get; set; }
    }
}
