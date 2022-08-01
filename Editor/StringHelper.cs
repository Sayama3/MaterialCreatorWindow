using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sayama.MaterialCreatorWindow.Editor
{
    public static class StringHelper
    {
        public static string[] GetAllFiles(this string directory)
        {
            if (!Directory.Exists(directory)) return new string[0];
            string[] files = Directory.GetFiles(directory);
            //files = files.Where(file => !file.ToLower().EndsWith("meta")).ToArray();
            return files;
        }

        public static string[] GetAllSubFolders(this string directory,bool includeRootDirectory = false)
        {
            if (!Directory.Exists(directory)) return includeRootDirectory ? new[] {directory} : new string[0];
            List<string> folders = Directory.GetDirectories(directory).ToList();
            if(includeRootDirectory)folders.Add(directory);
            return folders.ToArray();
        }

        public static string GetLastPartPath(this string filePath)
        {
            return filePath.Split('/', '\\').Last();
        }

        public static string GetFileNameNoExtension(this string fileName)
        {
            char separator = '.';
            if (!fileName.Contains(separator)) return fileName;
            string[] split = fileName.Split();
            return String.Join(".", split, 0, split.Length - 1);
        }

        public static string GetNPart(this string str, int index, char separator)
        {
            var split = str.Split(separator);
            return split.Length >= index + 1 ? split[index] : String.Empty;
        }
        //Source : https://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
        public static string AddSpacesToSentence(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }
    }
}