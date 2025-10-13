using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

namespace AIWebAPI.Interfaces;

public interface IFunctionToolService
{
    Task<string> ExecuteFunctionAsync(FunctionCall functionCall);
}