namespace Shiny.Extensions.Serialization.Tests;


[Collection("ShinyJson")]
public class SerializationTests
{
    [Fact(DisplayName = "Json - [ShinyJsonContext] host context auto-registers via module init")]
    public void AutoType_ElementRoundTrips()
    {
        var input = new AutoType { Title = "hello", Created = DateTimeOffset.UnixEpoch };
        var json = Json.Default.Serialize(input);
        Json.Default.Deserialize<AutoType>(json).Title.ShouldBe("hello");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables List<T> via generated collection resolver")]
    public void AutoType_List_RoundTrips()
    {
        var input = new List<AutoType> {
            new() { Title = "a" }, new() { Title = "b" }
        };
        var json = Json.Default.Serialize(input);
        var back = Json.Default.Deserialize<List<AutoType>>(json);
        back.Count.ShouldBe(2);
        back[1].Title.ShouldBe("b");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables T[] via generated collection resolver")]
    public void AutoType_Array_RoundTrips()
    {
        var input = new[] {
            new AutoType { Title = "x" }, new AutoType { Title = "y" }
        };
        var json = Json.Default.Serialize(input);
        var back = Json.Default.Deserialize<AutoType[]>(json);
        back.Length.ShouldBe(2);
        back[0].Title.ShouldBe("x");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables IEnumerable<T> via generated collection resolver")]
    public void AutoType_IEnumerable_RoundTrips()
    {
        IEnumerable<AutoType> input = new List<AutoType> { new() { Title = "i" } };
        var json = Json.Default.Serialize(input);
        var back = Json.Default.Deserialize<IEnumerable<AutoType>>(json);
        back.ShouldNotBeNull();
        back.First().Title.ShouldBe("i");
    }


    [Fact(DisplayName = "Json - element type without [ShinyJsonInclude] still round-trips but has no List wrapper")]
    public void SingleOnlyType_ElementRoundTrips_NoList()
    {
        // Element is in AppJsonContext, so single-instance serialization works.
        var json = Json.Default.Serialize(new SingleOnlyType { Value = "ok" });
        Json.Default.Deserialize<SingleOnlyType>(json).Value.ShouldBe("ok");

        // The list variant was never registered (no [ShinyJsonInclude]) and STJ has no metadata for List<SingleOnlyType>.
        Should.Throw<InvalidOperationException>(
            () => Json.Default.Serialize(new List<SingleOnlyType> { new() { Value = "x" } })
        );
    }


    [Fact(DisplayName = "Json - [assembly: ShinyJsonInclude] adds collection support for a foreign type")]
    public void ForeignType_FromAssemblyAttribute_HasCollectionSupport()
    {
        var input = new ForeignType { Foreign = "x", Number = 42 };
        var json = Json.Default.Serialize(input);
        Json.Default.Deserialize<ForeignType>(json).Foreign.ShouldBe("x");

        var list = new List<ForeignType> { input, new() { Foreign = "y", Number = 99 } };
        var listJson = Json.Default.Serialize(list);
        var back = Json.Default.Deserialize<List<ForeignType>>(listJson);
        back.Count.ShouldBe(2);
        back[1].Number.ShouldBe(99);
    }


    [Fact(DisplayName = "Json - inline JsonConverter composes with generated collection resolver")]
    public void BoxedInt_InlineConverter_AndCollection()
    {
        // Element-level inline converter: bare number, not an object.
        var single = Json.Default.Serialize(new BoxedInt { Value = 7 });
        single.ShouldBe("7");
        Json.Default.Deserialize<BoxedInt>(single)!.Value.ShouldBe(7);

        // Collection metadata from the generated resolver; inline converter applied per-element.
        var list = Json.Default.Serialize(new List<BoxedInt> {
            new() { Value = 1 }, new() { Value = 2 }, new() { Value = 3 }
        });
        list.ShouldContain("1");
        list.ShouldContain("2");
        list.ShouldContain("3");
        Json.Default.Deserialize<List<BoxedInt>>(list)[2].Value.ShouldBe(3);
    }


    [Fact(DisplayName = "Json - unknown type throws InvalidOperationException with a helpful message")]
    public void UnknownType_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(
            () => Json.Default.Serialize(new UnknownType { Anything = "x" })
        );
        ex.Message.ShouldContain("UnknownType");
    }


    // ---------- UTF-8 byte overloads ----------

    [Fact(DisplayName = "Json - SerializeToUtf8Bytes round-trips via Deserialize(ReadOnlySpan<byte>)")]
    public void Utf8_RoundTrips()
    {
        var input = new AutoType { Title = "utf8" };
        var bytes = Json.Default.SerializeToUtf8Bytes(input);

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);

        var back = Json.Default.Deserialize<AutoType>(bytes.AsSpan());
        back.Title.ShouldBe("utf8");
    }


    [Fact(DisplayName = "Json - UTF-8 byte overloads work for collection wrappers from [ShinyJsonInclude]")]
    public void Utf8_List_RoundTrips()
    {
        var input = new List<AutoType> { new() { Title = "a" }, new() { Title = "b" } };
        var bytes = Json.Default.SerializeToUtf8Bytes(input);
        var back = Json.Default.Deserialize<List<AutoType>>(bytes.AsSpan());

        back.Count.ShouldBe(2);
        back[1].Title.ShouldBe("b");
    }


    // ---------- Stream overloads ----------

    [Fact(DisplayName = "Json - SerializeAsync writes UTF-8 to stream and DeserializeAsync reads it back")]
    public async Task Stream_RoundTrips()
    {
        var input = new AutoType { Title = "stream" };

        using var ms = new MemoryStream();
        await Json.Default.SerializeAsync(ms, input);

        ms.Position = 0;
        var back = await Json.Default.DeserializeAsync<AutoType>(ms);

        back.ShouldNotBeNull();
        back!.Title.ShouldBe("stream");
    }


    [Fact(DisplayName = "Json - stream overloads work for collection wrappers")]
    public async Task Stream_List_RoundTrips()
    {
        var input = new List<AutoType> { new() { Title = "s1" }, new() { Title = "s2" } };

        using var ms = new MemoryStream();
        await Json.Default.SerializeAsync(ms, input);

        ms.Position = 0;
        var back = await Json.Default.DeserializeAsync<List<AutoType>>(ms);

        back.ShouldNotBeNull();
        back!.Count.ShouldBe(2);
        back[0].Title.ShouldBe("s1");
    }


    // ---------- Additional collection shapes ----------

    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables IReadOnlyList<T>")]
    public void AutoType_IReadOnlyList_RoundTrips()
    {
        var input = new List<AutoType> { new() { Title = "rol-a" }, new() { Title = "rol-b" } };
        var json = Json.Default.Serialize<IReadOnlyList<AutoType>>(input);
        var back = Json.Default.Deserialize<IReadOnlyList<AutoType>>(json);

        back.Count.ShouldBe(2);
        back[1].Title.ShouldBe("rol-b");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables IReadOnlyCollection<T>")]
    public void AutoType_IReadOnlyCollection_RoundTrips()
    {
        IReadOnlyCollection<AutoType> input = new List<AutoType> { new() { Title = "roc" } };
        var json = Json.Default.Serialize(input);
        var back = Json.Default.Deserialize<IReadOnlyCollection<AutoType>>(json);

        back.Count.ShouldBe(1);
        back.First().Title.ShouldBe("roc");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables IList<T>")]
    public void AutoType_IList_RoundTrips()
    {
        IList<AutoType> input = new List<AutoType> { new() { Title = "il" } };
        var json = Json.Default.Serialize(input);
        var back = Json.Default.Deserialize<IList<AutoType>>(json);

        back.Count.ShouldBe(1);
        back[0].Title.ShouldBe("il");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables ICollection<T>")]
    public void AutoType_ICollection_RoundTrips()
    {
        ICollection<AutoType> input = new List<AutoType> { new() { Title = "ic" } };
        var json = Json.Default.Serialize(input);
        var back = Json.Default.Deserialize<ICollection<AutoType>>(json);

        back.Count.ShouldBe(1);
        back.First().Title.ShouldBe("ic");
    }


    [Fact(DisplayName = "Json - [ShinyJsonInclude] enables IAsyncEnumerable<T> as a typed target")]
    public async Task AutoType_IAsyncEnumerable_Deserializes()
    {
        // Use the stream API so the source stream stays open while the async-enumerable consumer pulls.
        var input = new List<AutoType> { new() { Title = "ae1" }, new() { Title = "ae2" } };
        using var ms = new MemoryStream();
        await Json.Default.SerializeAsync(ms, input);
        ms.Position = 0;

        var enumerable = await Json.Default.DeserializeAsync<IAsyncEnumerable<AutoType>>(ms);
        enumerable.ShouldNotBeNull();

        var collected = new List<AutoType>();
        await foreach (var item in enumerable!)
            collected.Add(item);

        collected.Count.ShouldBe(2);
        collected[1].Title.ShouldBe("ae2");
    }


    [Fact(DisplayName = "Json - DeserializeAsyncEnumerable yields elements from a UTF-8 JSON array stream")]
    public async Task Stream_AsyncEnumerable_YieldsElements()
    {
        var input = new List<AutoType> {
            new() { Title = "e1" }, new() { Title = "e2" }, new() { Title = "e3" }
        };

        using var ms = new MemoryStream();
        await Json.Default.SerializeAsync(ms, input);
        ms.Position = 0;

        var collected = new List<AutoType>();
        await foreach (var item in Json.Default.DeserializeAsyncEnumerable<AutoType>(ms))
        {
            if (item is not null)
                collected.Add(item);
        }

        collected.Count.ShouldBe(3);
        collected[2].Title.ShouldBe("e3");
    }
}
