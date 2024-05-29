using BenchmarkDotNet.Attributes;
using Google.Api.Gax;
using Google.Cloud.Firestore;

namespace statestorebenchmark;

public class DatastoreVsFirestore
{
    private readonly FirestoreDb _firestore;

    private const string PropertyName = "flaffy";

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
    public async Task<string> FirestoreWriteAndRead()
    {
        string value = Guid.NewGuid().ToString("N");

        var docRef = _firestore.Document($"test/{value}");

        await docRef.SetAsync(new Dictionary<string, object>
        {
            { PropertyName, value }
        }, SetOptions.Overwrite);

        var snapshot = await docRef.GetSnapshotAsync();

        var result = snapshot.GetValue<string>(PropertyName);
        if (value != result)
            throw new Exception("values dont match");

        return result;
    }
}