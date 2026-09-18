using System;
using System.IO;
using Files = System.IO.File;

namespace SLS.SaveFileCore
{
    public class File
    {
        public string path { get; set; }
        public string filename { get; set; }
        public virtual string extension { get; set; }
        public File(string path, string filename, string extension)
        {
            this.path = path;
            this.filename = filename;
            this.extension = extension;
        }
        public string FullPath => Path.Combine(path, $"{filename}{extension}");
        public static implicit operator string(File obj) => obj?.FullPath;

        public virtual bool PathExists => Directory.Exists(path);
        public virtual bool FileExists => Files.Exists(FullPath);
        public virtual bool Exists => PathExists && FileExists;
        public virtual bool Valid => Exists;

        /// <summary>
        /// Deletes the file specified by this File's path and filename.
        /// </summary>
        public FileOpMessage DeleteFile()
        {
            if (!PathExists) return FileOpMessage.DirectoryNotFound;
            if (!FileExists) return FileOpMessage.FileNotFound;
            Files.Delete(FullPath);
            return FileOpMessage.Success;
        }
    }

    public class TextFile : File
    {
        public TextFile(string path, string filename) : base(path, filename, ".txt") { }

        /// <summary>
        /// Loads Json Data from the File specified by this JsonFile's path and filename.
        /// </summary>
        /// <returns>A <see cref="FileState"/> indicating the result of the load operation.</returns>
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

        /// <summary>  
        /// Saves the specified <see cref="NewData"/> content to the file specified by this JsonFile's path and filename.  
        /// </summary>  
        /// <param name="input">Quick override to input new/changed data before save.</param>  
        /// <returns>A <see cref="FileState"/> indicating the result of the operation.</returns>  
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

        public virtual bool HasData => FileExists && new FileInfo(FullPath).Length > 0;
        public override bool Valid => Exists && HasData;
    }

    public enum FileOpMessage
    {
        Success,
        FileVersionMismatch,
        FileMalformed,
        FileEmpty,
        FileNotFound,
        DirectoryNotFound,
        UnknownError
    }
}