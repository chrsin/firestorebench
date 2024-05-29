// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using statestorebenchmark;

BenchmarkRunner.Run<DatastoreVsFirestore>();

Console.WriteLine("Waiting 2 hours before exiting");
await Task.Delay(TimeSpan.FromHours(2));