namespace Netways.Logger.Model.Common
{
    public class Constants
    {
        public const string LangEn = "en";

        public const string LangAr = "ar";
    }

    public enum ErrorCodes
    {
        Success = 0,

        CrmError = 1,

        SystemError = 2,

        RecordNotFound = 3,

        ConfigurationNotExists = 4,

        TypeNotAllowed = 5,

        FileSizeGreaterThan1GB = 6,

        ValidationFailed = 7,
    }
    public enum Method
    {
        Get = 0,

        Post = 1,

        Delete = 2,

        Patch = 3,

        Put = 4,
    }
    public enum FileType
    {
        Docx = 1,
        Jpg = 2,
        Mp3 = 3,
        Mp4 = 4,
        Png = 5,
        Pdf = 6,
        PPtx = 7,
        Xla = 8,
        Xls = 9,
        Xlsb = 10,
        Xlsm = 11,
        Xltm = 12,
        Xltx = 13,
        Xlsx = 14,
        Php = 15,
        Zip = 16,
        Exe = 17,
        Rar = 18,
        Html = 19,
        Aspx = 20,
        Asp = 21,
        Py = 22,
        Csv = 23,
        Svg = 24,
        DWF = 25,
        DWG = 26,
    }

}
