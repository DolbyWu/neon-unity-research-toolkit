// Neon Research Toolkit: project-local research recording and review.
namespace NeonResearch
{
using System;
using System.IO;
using System.Text;
using System.Threading;

/// <summary>Publish complete local files. Never truncate the previous valid destination.</summary>
public static class StudyAtomicFile
{
    public static void Write(string path, string content)
    {
        string temp=path+".pending";
        File.WriteAllText(temp,content,new UTF8Encoding(false));
        Publish(temp,path);
    }
    public static void Publish(string temp,string path)
    {
        for(int attempt=0;;attempt++)
        {
            try
            {
                if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
                return;
            }
            catch(IOException) { if(attempt>=3)throw; Thread.Sleep(10*(attempt+1)); }
            catch(UnauthorizedAccessException) { if(attempt>=3)throw; Thread.Sleep(10*(attempt+1)); }
        }
    }
}

}
