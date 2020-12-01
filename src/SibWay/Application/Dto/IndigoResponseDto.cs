using Newtonsoft.Json;

namespace SibWay.Application.Dto
{
    public class IndigoResponseDto
    {
        public int Result { get;  }
        public string Message { get; }

        public IndigoResponseDto(int result, string message)
        {
            Result = result;
            Message = message;
        }


        public override string ToString()
        {
            var json=  JsonConvert.SerializeObject(this);
            return json;
        }
    }
}