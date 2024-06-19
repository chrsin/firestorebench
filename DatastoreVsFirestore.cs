using BenchmarkDotNet.Attributes;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;

namespace statestorebenchmark;

public class DatastoreVsFirestore
{
    private readonly FirestoreDb _firestore;

    private const string PropertyName = "Nested.Prop3";

    public DatastoreVsFirestore()
    {
        string? projectId = Environment.GetEnvironmentVariable("PROJECT_ID");
        if (projectId is null)
            throw new Exception("No PROJECT_ID env var supplied");
        EmulatorDetection emulatorDetection = EmulatorDetection.None;
        if (Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST") is not null)
            emulatorDetection = EmulatorDetection.EmulatorOnly;

        FirestoreDbBuilder firestoreDbBuilder = new FirestoreDbBuilder
        {
            EmulatorDetection = emulatorDetection,
            ProjectId = projectId
        };
        _firestore = firestoreDbBuilder.Build();
    }

    [Benchmark]
    public async Task<string> FirestoreWriteObjectAndRead()
    {
        string id = Guid.NewGuid().ToString("D");

        var docRef = _firestore.Document($"test/{id}");

        //Write the object to firestore
        var value = new { Prop1 = id, Nested = new { Prop1 = "np1", Prop2 = "np2" } };
        await docRef.SetAsync(value, SetOptions.MergeAll);

        //Update a property that doesn't exist on the nested object
        await docRef.SetAsync(new Dictionary<string, object>
        {
            { PropertyName, id }
        });

        value = value with { Nested = value.Nested with { Prop2 = "np2 changed" } };

        await docRef.SetAsync(value, SetOptions.MergeAll);

        var snapshot = await docRef.GetSnapshotAsync();
        VerifySnapshot(id, snapshot);

        return snapshot.Id;
    }

    [Benchmark]
    public async Task<string> FirestoreWriteSinglePropsAndRead()
    {
        string id = Guid.NewGuid().ToString("D");

        var docRef = _firestore.Document($"test/{id}");

        //Write the object to firestore
        await docRef.SetAsync(new Dictionary<string, object>
        {
            { nameof(Outer.Prop1), id },
            { $"{nameof(Outer.Nested)}.{nameof(Inner.Prop1)}", "np1" },
            { $"{nameof(Outer.Nested)}.{nameof(Inner.Prop2)}", "np2" }
        }, SetOptions.MergeAll);

        //Update a property that doesn't exist on the nested object
        await docRef.SetAsync(new Dictionary<string, object>
        {
            { PropertyName, id }
        }, SetOptions.MergeAll);

        await docRef.SetAsync(new Dictionary<string, object>
        {
            { $"{nameof(Outer.Nested)}.{nameof(Inner.Prop2)}", "np2 changed" }
        }, SetOptions.MergeAll);

        var snapshot = await docRef.GetSnapshotAsync();
        VerifySnapshot(id, snapshot);

        return snapshot.Id;
    }

    public bool VerifySnapshot(string id, DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();

        foreach (var keyPair in dict)
        {
            switch (keyPair.Key)
            {
                case nameof(Outer.Prop1):
                    if (keyPair.Value.ToString() != id)
                        throw new Exception(nameof(Outer.Prop1));
                    break;
                case $"{nameof(Outer.Nested)}.{nameof(Inner.Prop1)}":
                    if (keyPair.Value.ToString() != "np1")
                        throw new Exception($"{nameof(Outer.Nested)}.{nameof(Inner.Prop1)}");
                    break;
                case $"{nameof(Outer.Nested)}.{nameof(Inner.Prop2)}":
                    if (keyPair.Value.ToString() != "np2 changed")
                        throw new Exception($"{nameof(Outer.Nested)}.{nameof(Inner.Prop2)}");
                    break;
                case PropertyName:
                    if (keyPair.Value.ToString() != id)
                        throw new Exception(PropertyName);
                    break;
                default:
                    throw new Exception($"unknown field {keyPair.Key} with value {keyPair.Value}");
            }
        }

        return true;
    }

    public class Outer
    {
        public string? Prop1 { get; set; }
        public Inner? Nested { get; set; }
    }

    public class Inner
    {
        public string? Prop1 { get; set; }
        public string? Prop2 { get; set; }
    }
}