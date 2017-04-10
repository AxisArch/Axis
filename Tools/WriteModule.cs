using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Tools
{
    class WriteModule
    {
        static void WriteTextFile()
        {
            /*
            internal override void SaveCode(Program program, string folder)
            {
                if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($" Folder \"{folder}\" not found");
                Directory.CreateDirectory($@"{folder}\{program.Name}");

                for (int i = 0; i < program.Code.Count; i++)
                {
                    string group = MechanicalGroups[i].Name;

                    {
                        string file = $@"{folder}\{program.Name}\{program.Name}_{group}.MOD";
                        var joinedCode = string.Join("\r\n", program.Code[i][0]);
                        File.WriteAllText(file, joinedCode);
                    }

                    for (int j = 1; j < program.Code[i].Count; j++)
                    {
                        int index = j - 1;
                        string file = $@"{folder}\{program.Name}\{program.Name}_{group}_{index:000}.MOD";
                        var joinedCode = string.Join("\r\n", program.Code[i][j]);
                        File.WriteAllText(file, joinedCode);
                    }
                }
            }
            */
            // These examples assume a "C:\Users\Public\TestFolder" folder on your machine.
            // You can modify the path if necessary.


            // Example #1: Write an array of strings to a file.
            // Create a string array that consists of three lines.
            string[] lines = { "First line", "Second line", "Third line" };
            // WriteAllLines creates a file, writes a collection of strings to the file,
            // and then closes the file.  You do NOT need to call Flush() or Close().
            System.IO.File.WriteAllLines(@"C:\Users\Public\TestFolder\WriteLines.txt", lines);


            // Example #2: Write one string to a text file.
            string text = "A class is the most powerful data type in C#. Like a structure, " +
                           "a class defines the data and behavior of the data type. ";
            // WriteAllText creates a file, writes the specified string to the file,
            // and then closes the file.    You do NOT need to call Flush() or Close().
            System.IO.File.WriteAllText(@"C:\Users\Public\TestFolder\WriteText.txt", text);

            // Example #3: Write only some strings in an array to a file.
            // The using statement automatically flushes AND CLOSES the stream and calls 
            // IDisposable.Dispose on the stream object.
            // NOTE: do not use FileStream for text files because it writes bytes, but StreamWriter
            // encodes the output as text.
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"C:\Users\Public\TestFolder\WriteLines2.txt"))
            {
                foreach (string line in lines)
                {
                    // If the line doesn't contain the word 'Second', write the line to the file.
                    if (!line.Contains("Second"))
                    {
                        file.WriteLine(line);
                    }
                }
            }

            // Example #4: Append new text to an existing file.
            // The using statement automatically flushes AND CLOSES the stream and calls 
            // IDisposable.Dispose on the stream object.
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"C:\Users\Public\TestFolder\WriteLines2.txt", true))
            {
                file.WriteLine("Fourth line");
            }
        }
    }
}
