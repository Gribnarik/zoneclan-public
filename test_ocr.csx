#!/usr/bin/env dotnet-script
#r "nuget: SixLabors.ImageSharp, 3.1.12"
#r "nuget: FuzzySharp, 2.0.2"
#load "ZoneClan.Server/Features/Attendance/UploadTab/BusinessLogic/OcrService.cs"

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

// Mock IWebHostEnvironment
public class MockWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "/tmp";
    public IFileProvider WebRootFileProvider { get; set; }
    public string ApplicationName { get; set; } = "Test";
    public IFileProvider ContentRootFileProvider { get; set; }
    public string ContentRootPath { get; set; } = "/tmp";
    public string EnvironmentName { get; set; } = "Development";
}

var env = new MockWebHostEnvironment();
var service = new ZoneClan.Server.Features.Attendance.UploadTab.BusinessLogic.OcrService(env);

var tabsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Документы/проект/Табы");
var imageFiles = Directory.GetFiles(tabsFolder, "*.png");

Console.WriteLine($"Найдено {imageFiles.Length} скриншотов для тестирования\n");

var knownNicks = new List<string>
{
    "Gribnarik", "Ykpyfn", "Kolonich", "Palapenda",
    "ФААУСТ", "Ulti_Dem", "Ya_KOSTR", "Pendif"
};

foreach (var imagePath in imageFiles)
{
    Console.WriteLine($"=== Обработка: {Path.GetFileName(imagePath)} ===");
    try
    {
        var results = await service.ProcessTabAsync(imagePath, knownNicks);

        Console.WriteLine($"Распознано игроков: {results.Count}");
        Console.WriteLine("\nТоп 5 игроков:");
        foreach (var player in results.Take(5))
        {
            var knownMark = player.Known ? "✓" : "?";
            Console.WriteLine($"  {knownMark} {player.Nickname}: K:{player.Kills} D:{player.Deaths} A:{player.Assists} Score:{player.Score}");
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ОШИБКА: {ex.Message}\n");
    }
}

Console.WriteLine("Тестирование завершено!");
