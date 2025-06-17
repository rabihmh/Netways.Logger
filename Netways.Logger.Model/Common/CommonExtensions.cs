namespace Netways.Logger.Model.Common
{
    public static class CommonExtensions
    {

        public static bool IsJson(string input)
        {
            input = input.Trim();

            return input.StartsWith('{') && input.EndsWith('}') || input.StartsWith('[') && input.EndsWith(']');
        }
    }
}
