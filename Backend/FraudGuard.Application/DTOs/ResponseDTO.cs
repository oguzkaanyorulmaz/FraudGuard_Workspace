using System.Collections.Generic;

namespace FraudGuard.Application.DTOs
{
    public class ResponseDTO<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ResponseDTO<T> Success(T data, string message = "İşlem başarılı.")
        {
            return new ResponseDTO<T> { IsSuccess = true, Data = data, Message = message };
        }

        public static ResponseDTO<T> Fail(string error, string message = "İşlem başarısız.")
        {
            return new ResponseDTO<T> { IsSuccess = false, Message = message, Errors = new List<string> { error } };
        }
    }
}