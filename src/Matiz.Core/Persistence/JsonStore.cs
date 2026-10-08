using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Matiz.Core.Persistence;

/// <summary>Resultado de una carga: datos y si hubo que recuperarse de un archivo corrupto.</summary>
public sealed record LoadResult<T>(T Value, string? RecoveredCorruptFile);

/// <summary>
/// Almacén JSON de un único documento: escritura atómica (tmp + reemplazo con .bak), guardado diferido
/// (debounce) en segundo plano, recuperación de archivos corruptos y migraciones por "schemaVersion".
/// </summary>
public sealed class JsonStore<T> : IDisposable where T : class
{
    private readonly string _path;
    private readonly JsonTypeInfo<T> _typeInfo;
    private readonly Func<T> _factory;
    private readonly Func<JsonNode, JsonNode>? _migrate;
    private readonly TimeSpan _debounce;
    private readonly Lock _gate = new();
    private Timer? _timer;
    private string? _pendingJson;

    public JsonStore(string path, JsonTypeInfo<T> typeInfo, Func<T> factory,
        Func<JsonNode, JsonNode>? migrate = null, TimeSpan? debounce = null)
    {
        _path = path;
        _typeInfo = typeInfo;
        _factory = factory;
        _migrate = migrate;
        _debounce = debounce ?? TimeSpan.FromMilliseconds(300);
    }

    public string FilePath => _path;

    public LoadResult<T> Load()
    {
        if (!File.Exists(_path)) return new(_factory(), null);
        try
        {
            var text = File.ReadAllText(_path);
            var node = JsonNode.Parse(text) ?? throw new JsonException("Documento vacío");
            if (_migrate is not null) node = _migrate(node);
            var value = node.Deserialize(_typeInfo) ?? throw new JsonException("Documento nulo");
            return new(value, null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NotSupportedException or FormatException)
        {
            var name = Path.GetFileNameWithoutExtension(_path);
            var corrupt = Path.Combine(Path.GetDirectoryName(_path)!, $"{name}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(_path, corrupt, overwrite: true);
            return new(_factory(), corrupt);
        }
    }

    /// <summary>Serializa ya (en el hilo llamador) y programa la escritura diferida.</summary>
    public void ScheduleSave(T value)
    {
        var json = JsonSerializer.Serialize(value, _typeInfo);
        lock (_gate)
        {
            _pendingJson = json;
            _timer ??= new Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Guarda de inmediato (síncrono).</summary>
    public void Save(T value)
    {
        var json = JsonSerializer.Serialize(value, _typeInfo);
        lock (_gate)
        {
            _pendingJson = null;
            WriteAtomic(json);
        }
    }

    /// <summary>Escribe cualquier guardado pendiente.</summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (_pendingJson is null) return;
            var json = _pendingJson;
            _pendingJson = null;
            WriteAtomic(json);
        }
    }

    private void WriteAtomic(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(_path))
            File.Replace(tmp, _path, _path + ".bak", ignoreMetadataErrors: true);
        else
            File.Move(tmp, _path);
    }

    public void Dispose()
    {
        Flush();
        _timer?.Dispose();
    }
}
