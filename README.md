# Task-like Async Enumerators
## _a.k.a Abusing Task-like Types in C# 7_

Since async/await was first released I have often longed for 'async iterator' methods that could combine both the yield and async syntaxes.

While there are several options currently available for asynchronous sequences (Rx Observables, DataFlow blocks), none have the concise beauty of async/await and yield iterators. 

Async sequences are on the book for C# 8, and there are a number of [interesting discussions](https://github.com/dotnet/roslyn/issues/261) happening in the Rosyln issues.

But why wait! With C# 7's new Task-Like types we finally have the possibility to create async methods that return custom types. By capturing the underlying task-like object from within the method itself, we can return values _before_ the task has completed.

I believe this approach will have many cool applications. This repository contains my initial proof of concept.

> ### Warning! 
> I put this together as an experiment, there are likely better ways to approach most of the inner workings. I put zero emphasis on correctness, safety, or performance!

### Details

This repository contains several Task-like types that allow both cooporative and parallel async iterator methods. 

The `AsyncEnumerator<T>` class provides behavior similar to a standard yield iterator method except that it allows for asynchronous operations. Each `Yield(T)` call returns a value and asynchronously waits for the next call to `MoveNext()`:

``````````` c#

public static async AsyncEnumerator<int> Producer()
{
    var yield = await AsyncEnumerator<int>.Capture(); // Capture the underlying 'Task'

    await yield.Pause();             // Optionally wait for first MoveNext call

    await yield.Return(1);           // Yield the value and await MoveNext
                   
    await Task.Delay(100);           // Use any async constructs

    await yield.Return(2);

    return yield.Break();            // Return false to awaiting MoveNext
}

public static async Task Consumer()
{
    var p = Producer();                       

    while (await p.MoveNextAsync())       // Await the next value
    {
        Console.Write(" " + p.Current);   // Use the current value
    }
}

````````````

While cooperative iteration is great, I believe the more common desire with async code would be to have the producer run in its own thread and simply await the availability of results (closer in concept to observables). The `AsyncParallelEnumerator<T>` shown below does exactly this:

``````````` c#

public static async AsyncSequence<int> Producer2()
{
    var seq = await AsyncSequence<int>.Capture();            // Capture the underlying 'Task'
                       
    var users = await GetUsersAsync().ConfigureAwait(false); // Use any async constructs
    
    foreach(var user in users)
    {
        var fiends = await user.GetFriendsAsync(); // Build async 'flows' naturally 
        
        seq.Return(friends.Count);            // Signal an awaiting 
    }                                         // MoveNext, or queue the result.

    return seq.Break();                       // Complete the sequence and 
}                                             // return false to an awaiting MoveNext

public static async Task Consumer2()
{
    var p = Producer2();

    while (await p.MoveNextAsync())     // Await the next value
    {
        Console.WriteLine(p.Current);   // Use the current value
    }
}

```````````````

I also threw together several additional types with a similar approach. One is a `TaskLikeObservable` which allows you to write a _flat_ `IObservable` method such as:

`````````````` c#

public static async TaskLikeObservable<string> Producer()
{
    var o = await TaskLikeObservable<string>.Capture(); // Capture the underlying Task-like Obserable

    await o.Subscription;                               // wait for a subscriber

    for (var i = 0; i < 10; i++)
    {
        await Task.Delay(100).ConfigureAwait(false);    // Use normal async constructs
        
        o.OnNext("y" + i);                              // send the value
    }

    return o.OnCompleted();                             // complete the observable and return.
}

...

Producer().Subscribe(i => DoSomethingWith(i));          // Run and subscribe to the method

```````````````

Lastly I added a `CoopTask` class which allows the parent and child task to pass back and forth control similarly to a yielding enumerator, but of course with async constructs:

``````````` c#

public static async AsyncEnumerator<int> Producer()
{
    var yield = await AsyncEnumerator<int>.Capture(); // Capture the underlying 'Task'

    await yield.Pause();              // Optionally wait for first MoveNext call

    await yield.Return(1);            // Yield the value and wait for MoveNext
                   
    await Task.Delay(100);            // Use any async constructs

    await yield.Return(2);

    return yield.Break();             // Return false to awaiting MoveNext
}

public static async Task Consumer()
{
    var p = Producer();                       

    while (await p.MoveNextAsync())           // Await the next value
    {
        Console.WriteLine(" " + p.Current);   // Use the current value
    }
}

````````````

## Discussion

There are likely more clever ways of implementing most of the internals of these types. The 'hack' for capturing the underlying Task-like object is not as bad as I thought it would have to be - it originally used reflection - but the current method still relies on a 'dummy' continuation.

The main oddity I have run into is that the compiler appears to treat all generic Task-like methods as if they should behave like `Task<T>` even when their builder semantics actually follow the void returning `Task` approach. This means you have to 'return' a value even though the _Task_ and awaiter have no `Result` field. I guess this isn't the intended purpose...

I will be very curious to hear other developer's feedback. Please feel free to leave comments and feedback as issues. And of course, I welcome any pull requests for better code or more functionality.







