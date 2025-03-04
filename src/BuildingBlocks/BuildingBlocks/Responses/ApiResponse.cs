using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Responses
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Thành công !")
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T> Failure(string message = "Có lỗi xảy ra, vui lòng liên hệ bộ phận CNTT !", List<string> errors = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
