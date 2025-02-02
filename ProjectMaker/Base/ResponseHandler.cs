namespace ProjectMaker.Base
{
    public class ResponseHandler
    {
        public ResponseHandler()
        {

        }
        public Response<T> Deleted<T>(T entity, object meta = null)
        {
            return new Response<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = "Deleted Succefully",
                Meta = meta
            };
        }
        public Response<T> Success<T>(T entity, object meta = null)
        {
            return new Response<T>
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = "Success",
                Meta = meta
            };
        }
        public Response<T> UnAuthorized<T>()
        {
            return new Response<T>
            {
                StatusCode = System.Net.HttpStatusCode.Unauthorized,
                Succeeded = false,
                Message = "unAuthorized"
            };
        }
        public Response<T> BadRequest<T>(string message)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Succeeded = false,
                Message = message == string.Empty ? "Bad Request" : message
            };
        }
        public Response<T> UnprocessableEntity<T>(string message)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.UnprocessableEntity,
                Succeeded = false,
                Message = message == string.Empty ? "UnprocessableEntity" : message
            };
        }
        public Response<T> Created<T>(T entity, object? meta = null)
        {
            return new Response<T>()
            {
                Data = entity,
                StatusCode = System.Net.HttpStatusCode.Created,
                Succeeded = true,
                Message = "Created Successfully"
            };
        }


    }
}
