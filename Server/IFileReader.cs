using System.Threading.Tasks;

namespace MockApi.Server
{
    public interface IFileReader
    {
        Task<string> ReadContentsAsync(string file);
    }
}
