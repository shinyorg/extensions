using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Shiny.Extensions.Reactive.Tests;


public class RxExtensionsTests
{
    [Fact]
    public async Task WhenAnyProperty_Typed_EmitsInitialValueOnSubscribe()
    {
        var sample = new NotifyingSample { Name = "Initial", Age = 7 };

        var first = await sample
            .WhenAnyProperty(x => x.Name)
            .FirstAsync()
            .ToTask();

        first.ShouldBe("Initial");
    }


    [Fact]
    public void WhenAnyProperty_Typed_EmitsWhenMatchingPropertyChanges()
    {
        var sample = new NotifyingSample { Name = "First" };
        var values = new List<string?>();

        using var sub = sample.WhenAnyProperty(x => x.Name).Subscribe(values.Add);

        sample.Name = "Second";
        sample.Name = "Third";

        values.ShouldBe(new[] { "First", "Second", "Third" });
    }


    [Fact]
    public void WhenAnyProperty_Typed_IgnoresUnrelatedProperty()
    {
        var sample = new NotifyingSample { Name = "Name", Age = 1 };
        var values = new List<string?>();

        using var sub = sample.WhenAnyProperty(x => x.Name).Subscribe(values.Add);

        sample.Age = 99;

        values.ShouldBe(new[] { "Name" });
    }


    [Fact]
    public void WhenAnyProperty_Untyped_EmitsItemChangedForEveryProperty()
    {
        var sample = new NotifyingSample();
        var changes = new List<ItemChanged<NotifyingSample>>();

        using var sub = sample.WhenAnyProperty().Subscribe(changes.Add);

        sample.Name = "Test";
        sample.Age = 22;

        changes.Count.ShouldBe(2);
        changes[0].PropertyName.ShouldBe(nameof(NotifyingSample.Name));
        changes[0].GetValue().ShouldBe("Test");
        changes[1].PropertyName.ShouldBe(nameof(NotifyingSample.Age));
        changes[1].GetValue().ShouldBe(22);
    }


    [Fact]
    public void ItemChanged_GetValue_ReturnsNullWhenPropertyNameNull()
    {
        var sample = new NotifyingSample { Name = "X" };
        var change = new ItemChanged<NotifyingSample>(sample, null);

        change.GetValue().ShouldBeNull();
    }


    [Fact]
    public void SelectSwitch_SwitchesToLatestInnerObservable()
    {
        var outer = new Subject<int>();
        var inner1 = new Subject<string>();
        var inner2 = new Subject<string>();
        var values = new List<string>();

        using var sub = outer
            .SelectSwitch(x => x == 1 ? (IObservable<string>)inner1 : inner2)
            .Subscribe(values.Add);

        outer.OnNext(1);
        inner1.OnNext("from-inner1");

        outer.OnNext(2);
        inner1.OnNext("ignored-after-switch");
        inner2.OnNext("from-inner2");

        values.ShouldBe(new[] { "from-inner1", "from-inner2" });
    }


    [Fact]
    public async Task SelectAsync_NoToken_InvokesTaskAndPropagatesValue()
    {
        var result = await Observable
            .Return(0)
            .SelectAsync(() => Task.FromResult(42))
            .FirstAsync()
            .ToTask();

        result.ShouldBe(42);
    }


    [Fact]
    public async Task SelectAsync_WithToken_ReceivesCancellationToken()
    {
        CancellationToken capturedToken = default;

        var result = await Observable
            .Return(0)
            .SelectAsync(ct =>
            {
                capturedToken = ct;
                return Task.FromResult(99);
            })
            .FirstAsync()
            .ToTask();

        result.ShouldBe(99);
        capturedToken.CanBeCanceled.ShouldBeTrue();
    }


    [Fact]
    public void ObserveOnIf_NullScheduler_ReturnsSameObservable()
    {
        var source = Observable.Return(1);
        var result = source.ObserveOnIf(null);

        result.ShouldBeSameAs(source);
    }


    [Fact]
    public async Task ObserveOnIf_WithScheduler_AppliesObserveOn()
    {
        var source = Observable.Return(123);
        var result = source.ObserveOnIf(CurrentThreadScheduler.Instance);

        result.ShouldNotBeSameAs(source);
        var value = await result.FirstAsync().ToTask();
        value.ShouldBe(123);
    }


    [Fact]
    public void DoOnce_InvokesActionOnFirstValueOnly()
    {
        var invocations = new List<int>();

        Observable
            .Range(1, 5)
            .DoOnce(invocations.Add)
            .Subscribe();

        invocations.ShouldBe(new[] { 1 });
    }


    [Fact]
    public void DoOnce_PassesThroughAllValues()
    {
        var passedThrough = new List<int>();

        Observable
            .Range(1, 3)
            .DoOnce(_ => { })
            .Subscribe(passedThrough.Add);

        passedThrough.ShouldBe(new[] { 1, 2, 3 });
    }


    [Fact]
    public void DisposedBy_AddsToCompositeAndReturnsOriginal()
    {
        using var composite = new CompositeDisposable();
        var disposable = Disposable.Create(() => { });

        var returned = disposable.DisposedBy(composite);

        returned.ShouldBeSameAs(disposable);
        composite.Count.ShouldBe(1);
    }


    [Fact]
    public void DisposedBy_DisposesWhenCompositeDisposes()
    {
        var composite = new CompositeDisposable();
        var disposed = false;
        Disposable.Create(() => disposed = true).DisposedBy(composite);

        composite.Dispose();

        disposed.ShouldBeTrue();
    }


    [Fact]
    public void Respond_EmitsValueAndCompletes()
    {
        var subject = new Subject<int>();
        var values = new List<int>();
        var completed = false;

        using var sub = subject.Subscribe(values.Add, () => completed = true);

        ((IObserver<int>)subject).Respond(7);

        values.ShouldBe(new[] { 7 });
        completed.ShouldBeTrue();
    }


    [Fact]
    public async Task SubscribeAsync_ProcessesValuesSequentially()
    {
        var source = new Subject<int>();
        var order = new List<string>();

        using var sub = source.SubscribeAsync(async x =>
        {
            order.Add($"start-{x}");
            await Task.Delay(50);
            order.Add($"end-{x}");
        });

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        await Task.Delay(300);

        order.ShouldBe(new[] { "start-1", "end-1", "start-2", "end-2" });
    }


    [Fact]
    public async Task SubscribeAsync_WithError_InvokesErrorHandler()
    {
        var source = new Subject<int>();
        Exception? captured = null;

        using var sub = source.SubscribeAsync(
            _ => throw new InvalidOperationException("boom"),
            ex => captured = ex
        );

        source.OnNext(1);

        await Task.Delay(100);

        captured.ShouldBeOfType<InvalidOperationException>();
        captured!.Message.ShouldBe("boom");
    }


    [Fact]
    public async Task SubscribeAsync_WithCompleteHandler_InvokesOnComplete()
    {
        var source = new Subject<int>();
        var completed = false;

        using var sub = source.SubscribeAsync(
            _ => Task.CompletedTask,
            _ => { },
            () => completed = true
        );

        source.OnNext(1);
        source.OnCompleted();

        await Task.Delay(100);

        completed.ShouldBeTrue();
    }


    [Fact]
    public async Task SubscribeAsyncConcurrent_AllowsOverlappingExecutions()
    {
        var source = new Subject<int>();
        var inFlight = 0;
        var maxInFlight = 0;
        var gate = new object();

        using var sub = source.SubscribeAsyncConcurrent(async _ =>
        {
            lock (gate)
            {
                inFlight++;
                if (inFlight > maxInFlight)
                    maxInFlight = inFlight;
            }
            await Task.Delay(100);
            lock (gate) inFlight--;
        });

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        await Task.Delay(400);

        maxInFlight.ShouldBeGreaterThan(1);
    }


    [Fact]
    public async Task SubscribeAsyncConcurrent_WithMaxConcurrent_RespectsLimit()
    {
        var source = new Subject<int>();
        var inFlight = 0;
        var maxInFlight = 0;
        var gate = new object();

        using var sub = source.SubscribeAsyncConcurrent(async _ =>
        {
            lock (gate)
            {
                inFlight++;
                if (inFlight > maxInFlight)
                    maxInFlight = inFlight;
            }
            await Task.Delay(100);
            lock (gate) inFlight--;
        }, maxConcurrent: 2);

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);
        source.OnNext(4);

        await Task.Delay(500);

        maxInFlight.ShouldBeLessThanOrEqualTo(2);
    }
}


file class NotifyingSample : INotifyPropertyChanged
{
    string? name;
    int age;

    public string? Name
    {
        get => this.name;
        set
        {
            this.name = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Name)));
        }
    }

    public int Age
    {
        get => this.age;
        set
        {
            this.age = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Age)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
