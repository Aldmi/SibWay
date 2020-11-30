using CSharpFunctionalExtensions;

namespace SibWay.Application.EventHandlers
{
    public class SibWaySendDataResponse
    {
        public SibWaySendDataResponse(Result result)
        {
            Result = result;
        }
        public Result Result { get; }
    }
}