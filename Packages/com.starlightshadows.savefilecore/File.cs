using System;
using System.IO;
using Files = System.IO.File;

namespace SLS.SaveFileCore
{
    /// <summary>Represents a file as an object using its directory, base name, and extension.</summary>
    public class File
    {
        /// <summary>Gets or sets the directory containing the file.</summary>
        public string path { get; set; }
        /// <summary>Gets or sets the file's base name, without its extension.</summary>
        public string filename { get; set; }
        /// <summary>Gets or sets the file extension, including its leading period.</summary>
        public virtual string extension { get; set; }

        /// <summary>Initializes a file reference from its directory, base name, and extension.</summary>
        /// <param name="path">Directory containing the file.</param>
        /// <param name="filename">File's base name, without its extension.</param>
        /// <param name="extension">File extension, including its leading period.</param>
        public File(string path, string filename, string extension)
        {
            this.path = path;
            this.filename = filename;
            this.extension = extension;
        }

        /// <summary>Gets the complete path formed from the directory, base name, and extension.</summary>
        public string FullPath => Path.Combine(path, $"{filename}{extension}");

        /// <summary>Converts a file reference to its full path; returns <see langword="null"/> for a null reference.</summary>
        /// <param name="obj">File reference to convert.</param>
        /// <returns>The file's full path, or <see langword="null"/> if the reference is null.</returns>
        public static implicit operator string(File obj) => obj?.FullPath;

        /// <summary>Gets whether the parent directory exists.</summary>
        public virtual bool PathExists => Directory.Exists(path);
        /// <summary>Gets whether the file exists at <see cref="FullPath"/>.</summary>
        public virtual bool FileExists => Files.Exists(FullPath);
        /// <summary>Gets whether both the parent directory and file exist.</summary>
        public virtual bool Exists => PathExists && FileExists;
        /// <summary>Gets whether this file reference is valid; the base implementation checks <see cref="Exists"/>.</summary>
        public virtual bool Valid => Exists;

        /// <summary>Deletes the file at this file reference's full path.</summary>
        /// <returns>The result of the delete operation.</returns>
        public FileOpMessage DeleteFile()
        {
            if (!PathExists) return FileOpMessage.DirectoryNotFound;
            if (!FileExists) return FileOpMessage.FileNotFound;
            Files.Delete(FullPath);
            return FileOpMessage.Success;
        }
    }

    /// <summary>Represents a text file with UTF-8 text load and save operations.</summary>
    public class TextFile : File
    {
        /// <summary>Initializes a text file reference with the <c>.txt</c> extension.</summary>
        /// <param name="path">Directory containing the file.</param>
        /// <param name="filename">File's base name, without its extension.</param>
        public TextFile(string path, string filename) : base(path, filename, ".txt") { }

        /// <summary>Reads the complete text content from this file.</summary>
        /// <param name="result">Receives the file contents, or <see langword="null"/> when the file cannot be read.</param>
        /// <returns>The result of the load operation.</returns>
        public virtual FileOpMessage LoadFromFile(out string result)
        {
            result = null;
            try
            {

                if (!PathExists) return FileOpMessage.DirectoryNotFound;
                if (!FileExists) return FileOpMessage.FileNotFound;

                using StreamReader load = Files.OpenText(FullPath);
                result = load.ReadToEnd();

                return string.IsNullOrWhiteSpace(result)
                    ? FileOpMessage.FileEmpty
                    : FileOpMessage.Success;
            }
            catch (DirectoryNotFoundException) { return FileOpMessage.DirectoryNotFound; }
            catch (FileNotFoundException) { return FileOpMessage.FileNotFound; }
            catch (Exception) { result = null; return FileOpMessage.UnknownError; }
        }

        /// <summary>Saves text content to this file, optionally creating its directory.</summary>
        /// <param name="input">Text content to write.</param>
        /// <param name="createIfNonexistant">Whether to create the directory when it does not exist.</param>
        /// <returns>The result of the save operation.</returns>
        public virtual FileOpMessage SaveToFile(string input, bool createIfNonexistant = true)
        {
            try
            {
                if (createIfNonexistant)
                {
                    if (!PathExists) Directory.CreateDirectory(path);
                }
                else
                {
                    if(!FileExists) return FileOpMessage.FileNotFound;
                    if(!PathExists) return FileOpMessage.DirectoryNotFound;
                }
                using StreamWriter file = Files.CreateText(FullPath);
                file.WriteLine(input);
                return FileOpMessage.Success;
            }
            catch (DirectoryNotFoundException) { return FileOpMessage.DirectoryNotFound; }
            catch (FileNotFoundException) { return FileOpMessage.FileNotFound; }
            catch (Exception) { return FileOpMessage.UnknownError; }
        }

        /// <summary>Gets whether this file exists and has a nonzero length.</summary>
        public virtual bool HasData => FileExists && new FileInfo(FullPath).Length > 0;
        /// <summary>Gets whether this file exists and contains data.</summary>
        public override bool Valid => Exists && HasData;
    }

    /// <summary>Describes the outcome of a file operation.</summary>
    public enum FileOpMessage
    {
        /// <summary>The operation completed successfully.</summary>
        Success,
        /// <summary>The file contains a version that does not match the requested version.</summary>
        FileVersionMismatch,
        /// <summary>The file contents are malformed or cannot be identified.</summary>
        FileMalformed,
        /// <summary>The file contains no data.</summary>
        FileEmpty,
        /// <summary>The requested file does not exist.</summary>
        FileNotFound,
        /// <summary>The directory containing the file does not exist.</summary>
        DirectoryNotFound,
        /// <summary>The operation failed for an unspecified reason.</summary>
        UnknownError
    }
}