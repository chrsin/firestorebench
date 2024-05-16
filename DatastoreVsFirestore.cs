using BenchmarkDotNet.Attributes;
using Google.Api.Gax;
using Google.Cloud.Datastore.V1;
using Google.Cloud.Firestore;

namespace statestorebenchmark;

public class DatastoreVsFirestore
{
    private readonly DatastoreDb _datastore;
    private readonly FirestoreDb _firestore;

    private const string PropertyName = "flaffy";

    public DatastoreVsFirestore()
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8978");
        FirestoreDbBuilder firestoreDbBuilder = new FirestoreDbBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly,
            ProjectId = "default"
        };
        _firestore = firestoreDbBuilder.Build();

        Environment.SetEnvironmentVariable("DATASTORE_EMULATOR_HOST", "localhost:8380");
        DatastoreDbBuilder datastoreDbBuilder = new DatastoreDbBuilder
        {
            EmulatorDetection = EmulatorDetection.EmulatorOnly,
            ProjectId = "default"
        };

        _datastore = datastoreDbBuilder.Build();
    }

    [Benchmark]
    public async Task<string> Datastore()
    {
        string value = Guid.NewGuid().ToString("N");
        var keyFactory = _datastore.CreateKeyFactory("yolo");

        var key = keyFactory.CreateKey(value);

        await _datastore.InsertAsync(new Entity
        {
            Key = key,
            Properties =
            {
                {
                    PropertyName, new Value
                    {
                        StringValue = value
                    }
                }
            }
        });

        var result = await _datastore.LookupAsync(key);
        var resultValue = result.Properties[PropertyName].StringValue;
        if (resultValue != value)
            throw new Exception("values dont match");

        return resultValue;
    }

    [Benchmark]
    public async Task<string> Firestore()
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