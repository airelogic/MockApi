using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace MockApi.Server
{
    public class FileSystemFileReader : IFileReader
    {
        private string _basePath;

        public FileSystemFileReader(IOptions<FileReaderOptions> fileReaderOptions)
        {
            _basePath = fileReaderOptions.Value.Root;
        }

        public Task<string> ReadContentsAsync(string file)
        {
            var invalidChars = new[] { ':', '<', '>', '?', '/', '\\', '*', '|' };
            foreach (var invalidChar in invalidChars)
                file = file.Replace(invalidChar, '_');
            return System.IO.File.ReadAllTextAsync(Path.Combine(_basePath, file));
        }
    }
}
