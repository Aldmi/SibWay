using CSharpFunctionalExtensions;

namespace SibWay.Application.EventHandlers
{
    public class SibWayResponseItem
    {
        public SibWayResponseItem(Result result)
        {
            Result = result;
        }
        public Result Result { get; }
    }
}