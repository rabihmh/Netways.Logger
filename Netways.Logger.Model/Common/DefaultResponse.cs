namespace Netways.Logger.Model.Common
{

    public class DefaultResponse<T>
    {
        public DefaultResponse()
        {

        }

        public DefaultResponse(string errorMessageEn, int errorCode)
        {
            IsSuccess = false;

            IsSystemError = true;

            ErrorMessageEn = errorMessageEn;

            Code = errorCode;
        }

        public T? Result { get; set; }

        public bool IsSuccess { get; set; } = true;

        public bool IsSystemError { get; set; } = false;

        public string ErrorMessageEn { get; set; } = "";

        public string ErrorMessageAr { get; set; } = "";

        public int Code { get; set; } = 0;

        public static implicit operator DefaultResponse<T>(T result)
        {
            return new DefaultResponse<T>
            {
                Result = result,
                IsSuccess = true
            };
        }
    }
}
