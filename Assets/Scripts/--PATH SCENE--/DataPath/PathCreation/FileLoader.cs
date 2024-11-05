using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

public static class FileLoader {
    
    #region FILE_ENDING_LIBRARIES

    // image, video, audio file endings
    private static readonly string[] ImageFileEnding = { // list of image file endings. incomplete
        ".jpg", ".jpeg", ".jpe", ".jif", ".jfif", ".jfi", ".png", ".webp", ".psd",
    };
    private static readonly string[] VideoFileEnding = { // list of video file endings. incomplete
        ".mp4", ".mov",
    };
    private static readonly string[] AudioFileEnding = { // list of audio file endings. incomplete
        ".mp3", ".wav",
    };

    //private static bool useStreamAssets = true;
    
    #endregion
    
    
    // loads file from Resource folder and returns Texture2D / VideoClip / AudioClip
    public static Object LoadMediaFile
    (string rawPath, string pathToParentDirectory, VideoImportManager.VideoSourceTypes videoSourceType, 
        string serverURL, bool searchDirectory = true, bool enableLog = true) {

        var cleanPath = CleanFilePath(rawPath, pathToParentDirectory);
        //if (cleanPath == null) return null;

        
        Object medium;
        // if file is image: load as Texture2D
        if (StringContainsWordOf(rawPath, ImageFileEnding)) {
            medium = LoadResourceAsAsset<Texture2D>(cleanPath, enableLog);
            
            // if we cannot find the file at the location given in the json file
            // we can still search for it in the paths directory (enabled with searchDirectory = true)
            
            if (medium == null && searchDirectory)
                medium = TryFindAndLoadAsAsset<Texture2D>(cleanPath, pathToParentDirectory, enableLog);
        }
        
        // if file is video:
        else if (StringContainsWordOf(rawPath, VideoFileEnding)) {

            switch (videoSourceType) {
                case VideoImportManager.VideoSourceTypes.VideoClip:
                    //  load as VideoClip
                    medium = LoadResourceAsAsset<VideoClip>(cleanPath, enableLog);
                    if (medium == null && searchDirectory)
                        medium = TryFindAndLoadAsAsset<VideoClip>(cleanPath, pathToParentDirectory, enableLog);
                    break;
                case VideoImportManager.VideoSourceTypes.URL:
                    // load as URL and save in url container
                    var urlContainer = ScriptableObject.CreateInstance<UrlContainer>();
                    urlContainer.url = GetVideoURL(rawPath, pathToParentDirectory, serverURL);
                    medium = urlContainer;
                    break;
                default:
                    return null;
            }
            
        }
        
        // if file is audio: load as AudioClip
        else if (StringContainsWordOf(rawPath, AudioFileEnding)) {
            medium = LoadResourceAsAsset<AudioClip>(cleanPath, enableLog);

            if (medium == null && searchDirectory)
                medium = TryFindAndLoadAsAsset<AudioClip>(cleanPath, pathToParentDirectory, enableLog);
        }

        
        else {
            Debug.Log("Could not determine type of" + cleanPath);
            return null;
        }
        
        
        return medium;
    }


    private static string GetVideoURL(string rawPath, string pathToParentDirectory, string serverURL,
        bool enableLog = true) {
        
        // get cleanPath (eg: pathPackage/contentPoint/file.mp4)
        var cleanPath = CleanFilePath(rawPath, pathToParentDirectory, removeFileEnding: false);
        
        // 
        var url = "";
        // A) url is online
        if (serverURL.Contains("http")) {
            // add serverAddress to url
            url = serverURL + "/" + cleanPath;
            
        }else if (rawPath.Contains("http")) {
            // url already complete
            url = rawPath;
        }
        // B) url is local -> StreamingAssets
        else {
            url = cleanPath;    // cleanPath (location in StreamingAssets) gets expanded to complete in runtime
        }

        // return without log
        if (!enableLog)
            return url;

        // return with log
        Debug.Log("Set Video URL: " + url);
        return url;
    }


    private static UnityEngine.Object LoadResourceAsAsset<T>(string path, bool logMessage = true)
        where T : UnityEngine.Object {

        var asset = Resources.Load<T>(path);
        
        if (!logMessage) return asset;
        
        
        // log:
        // :(
        if (!asset) Debug.Log("<color=red>Could not load " + typeof(T) + " : " + path +"</color>");
        // :)
        else Debug.Log("<color=green>Loaded : " + asset + " : " + path + "</color>");

        return asset;
    }


    private static string CleanFilePath(string path, string pathToParentDirectory, bool removeFileEnding = true,
        string[] ignoredPaths = null, string[] skippedWords = null) {
        // add parent directory
        var cleanPath = pathToParentDirectory + "/" + path;

        // remove "Assets/Resources/"
        cleanPath = cleanPath.Replace("Assets/Resources/", "");

        // remove file ending
        if (removeFileEnding)
            cleanPath = cleanPath.Split(".")[0];


        // remove words from filepath
        skippedWords ??= new string[] { "saved/", "icons//", "public/" }; // default value if skippedWords == null
        foreach (var str in skippedWords) {
            cleanPath = cleanPath.Replace(str, "");
        }

        // ignore file with path containing word of
        ignoredPaths ??= new string[] { };
        foreach (var str in skippedWords) {
            if (cleanPath.Contains(str)) return null;
        }

        return cleanPath;
    }



    private static bool StringContainsWordOf(string s, IEnumerable<string> words) {
        foreach (var w in words) {
            if (s.Contains(w))
                return true;
        }

        return false;
    }


    
    #region SEARCH_DIRECTIRY_FUNCTIONS

    private static UnityEngine.Object TryFindAndLoadAsAsset<T>(string cleanPath, string pathToParentDirectory, bool logMessage)
        where T : UnityEngine.Object {
        
        var path = SearchDirectoryForFilePath(cleanPath, logMessage);
        if (path == null) return null;
        
        
        var cleanedPath = CleanFilePath(path, "");
        if (cleanedPath == null) return null;
        

        // remove file ending
        path = path.Split(".")[0];
        
        // remove assets/resource
        path = path.Replace("Assets/Resources/", "");
        
        return LoadResourceAsAsset<T>(path, logMessage);
        
    }
    
    
    // search for file in Assets/Resources/[Subfolder]/...
    private static List<string> GetAllFilePathsInDirectory(string directoryName) {
        var paths = new List<string>();
        //LoadPathsRecursive(path: "Assets/Resouces/" + directoryName, ref paths);
        
        return paths;
    }

    private static string SearchDirectoryForFilePath(string cleanPath, bool logMessage, List<string> preLoadedPaths = null) {
        
        // get path directory form clean path:
        var directoryName = cleanPath.Split("/")[0];
        
        // get file name from clean path:
        var filename = cleanPath.Split("/")[^1];
        
        if(logMessage)
            Debug.Log("Searching for " + filename + " in " + directoryName + " ...");
        
        // if paths are not preloaded. load now
        if (preLoadedPaths == null) 
            preLoadedPaths = GetAllFilePathsInDirectory(directoryName);
        
        
        // search for file in paths
        foreach (var p in preLoadedPaths) {
            if (StringContainsWordOf(p, new List<string> { filename }))
                return p;
        }

        if (logMessage)
            Debug.Log("<color=red>" + 
                      "could not find " + filename + 
                      "</color>");
        
        return null;
        
    }
    
    
    // search for file in the whole Resources/ filesystem
    // https://stackoverflow.com/questions/57094126/searching-through-all-subfolders-when-using-resources-load-in-unity
    private static void LoadPathsRecursive(string path, ref List<string> paths) {
 
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        // BUG: path not found ? 
        if(!dirInfo.Exists) {
            Debug.Log("LoadPathsRecurseive().dirInfo : " + dirInfo + " does not exist >:(");
            return;
        }

        foreach (var file in dirInfo.GetFiles()) {
            if (file.Name.Contains(".") && !file.Name.Contains(".meta")) {
                paths.Add(path + "/" + file.Name);
            }
        }

        foreach (var dir in dirInfo.GetDirectories()) {
            LoadPathsRecursive(path + "/" + dir.Name, ref paths);
        }
    } 

    #endregion
    
}