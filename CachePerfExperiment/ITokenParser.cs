using System.Threading.Tasks;

namespace CachePerfExperiment
{
    interface ITokenParser
    {
        Task<string> ParseAsync(string token);
    }
}
