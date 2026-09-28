using System.Threading.Tasks;
using ProductService.Application.DTOs.Common;

namespace ProductService.Application.Interfaces.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file and returns its relative URL/path.
        /// </summary>
        Task<string> SaveFileAsync(FileData fileData, string directory);

        /// <summary>
        /// Deletes a file given its relative URL/path.
        /// </summary>
        Task DeleteFileAsync(string relativeFilePath);
    }
}

