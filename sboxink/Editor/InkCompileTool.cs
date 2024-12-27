using Editor;
using Ink;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public static class InkCompileTool
{
    private static FileSystemWatcher _watcher = null;

    // queue for ink files to compile
    private static List<string> inkFilesToCompile = new();

    [Menu("Editor", "Ink/Compile All Ink Files")]
    [Shortcut("EditorTool.force_compile_ink_files", "ctrl+shift+i")]
    public static void CompileAllInkFiles()
    {
        try
        {
            string pathToWatch = FileSystem.Mounted.GetFullPath("ink");
            string[] files = Directory.GetFiles(pathToWatch, "*.ink", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                CompileInk(file);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error compiling all ink files: {ex.Message}");
        }

        inkFilesToCompile.Clear();
        StartWatching();

    }


    [EditorEvent.Hotload]
    public static void OnHotload()
    {
        CompileAllInkFiles();
    }

    [EditorEvent.Frame]
    public static void OnFrame()
    {
        if (Application.FocusWidget == null)
            return;

        // Log.Info($"Ink files to compile: {_inkFilesToCompile.Count}");
        // compile all ink in the queue
        for (int i = 0; i < inkFilesToCompile.Count; i++)
        {
            string path = inkFilesToCompile[i];
            Log.Info($"In the queue: {path}");
            if ( IsFileReady(path) )
            {
                CompileInk(path);
                inkFilesToCompile.RemoveAt(i);
            }
            else
            {
                Log.Warning($"File {path} is not ready yet");
                continue;
            }
        }

    }

    private static bool IsFileReady( string path )
    {
        try
        {
            using ( FileStream stream = File.Open( path, FileMode.Open, FileAccess.Read, FileShare.None ) )
            {
                return stream.Length > 0;
            }
        }
        catch ( IOException )
        {
            Log.Warning( $"File {path} is not ready yet" );
            return false;
        }
    }

    public static void CompileInk(string path_to_compile)
    {
        string filename = Path.GetFileName(path_to_compile);
        Stopwatch stopwatch = new();
        stopwatch.Start();
        Log.Info($"Started compiling {filename} at {DateTime.Now}");
        try
        {
            var ink_source = File.ReadAllText(path_to_compile);
            var compiler = new Compiler(ink_source, new Compiler.Options
            {
                countAllVisits = true,
                fileHandler = new SboxInkFileHandler(Path.GetDirectoryName(path_to_compile)),
                errorHandler = (string message, ErrorType type) =>
                {
                    Log.Warning($"Ink error: {message}");
                }
            });
            var compiled = compiler.Compile();
            var json = compiled.ToJson();
            // Log.Info(json);

            var json_path = path_to_compile + ".json";
            File.WriteAllText(json_path, json);
        }
        catch (Exception ex)
        {
            Log.Error($"Error compiling ink: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            Log.Info($"Finished compiling {path_to_compile} in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    [Menu("Editor", "Ink/Start watching for changes")]
    public static void StartWatching()
    {
        try
        {
            if ( _watcher != null )
            {
                Log.Info( "Watcher already exists, disposing..." );
                _watcher.Dispose();
            }

            string pathToWatch = FileSystem.Mounted.GetFullPath( "ink" );
            _watcher = new FileSystemWatcher
            {
                Path = pathToWatch,
                Filter = "*.ink", // Watch only files ending with .ink
                IncludeSubdirectories = true, // Include subdirectories
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName // Watch for changes in file content and name
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnDeleted;

            _watcher.EnableRaisingEvents = true;

            Log.Info( $"Watching directory: {pathToWatch}" );
        }
        catch (Exception ex)
        {
            Log.Error($"Error starting watcher: {ex.Message}");
        }
    }


    private static void OnDeleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            Log.Info($"File {e.ChangeType}: {e.FullPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error handling file change: {ex.Message}");
        }
    }

    private static void OnChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (!inkFilesToCompile.Contains(e.FullPath)) {
                inkFilesToCompile.Add(e.FullPath);
                Log.Info($"Added {e.FullPath} to the compile queue ({e.ChangeType})");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error handling file change: {ex.Message}");
        }
    }
}